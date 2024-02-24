using Microsoft.Extensions.Logging;
using Pkl.Decoding;
using Pkl.EvaluatorManager;
using Pkl.InternalMsgApi.Incoming;
using Pkl.InternalMsgApi.Outgoing;

namespace Pkl.Evaluation;

public class Evaluator : IEvaluator
{
    private readonly long _evaluatorId;
    private readonly IEvaluatorManager _evaluatorManager;
    private readonly Decoder _decoder;
    private bool _closed;
    private readonly ILogger _logger;

    public Evaluator(long evaluatorId, IEvaluatorManager evaluatorManager, Decoder decoder, ILogger logger)
    {
        _evaluatorId = evaluatorId;
        _evaluatorManager = evaluatorManager;
        _decoder = decoder;
        _logger = logger;
    }

    public async Task<T> EvaluateModule<T>(ModuleSource source) where T : notnull
    {
        return await EvaluateExpression<T>(source, null);
    }

    public async Task<string> EvaluateOutputText(ModuleSource source)
    {
        return await EvaluateExpression<string>(source, "output.text");
    }

    public async Task<T> EvaluateOutputValue<T>(ModuleSource source) where T : notnull
    {
        return await EvaluateExpression<T>(source, "output.value");
    }

    public async Task<T> EvaluateExpression<T>(ModuleSource source, string? expr) where T : notnull
    {
        var raw = await EvaluateExpressionRaw(source, expr);
        return _decoder.Decode<T>(raw);
    }

    public async Task<byte[]> EvaluateExpressionRaw(ModuleSource source, string? expr)
    {
        if (_closed)
        {
            throw new InvalidOperationException("Evalutor cannot evaluate because it is closed");
        }
        
        var evaluate = new Evaluate
        {
            EvaluatorId = _evaluatorId,
            RequestId = 1, // TODO
            ModuleUri = source.Uri.ToString(),
            ModuleText = source.Contents,
            Expr = expr
        };

        var response = await _evaluatorManager.Send(evaluate, evaluate.RequestId);
        var evaluateResponse = (response as EvaluateResponse)!;

        if (!string.IsNullOrEmpty(evaluateResponse.Error) || evaluateResponse.Result is null)
        {
            throw new Exception(evaluateResponse.Error);
        }

        return evaluateResponse.Result;
    }

    public void Close()
    {
        if (_closed)
        {
            return;
        }

        _closed = true;
        _evaluatorManager.CloseEvaluator(_evaluatorId);
    }

    public void HandleLog(Log log)
    {
        const string template = "{evaluationLogMessage} - {evaluationLogFrameUri}";
        // levels other than 0 and 1 are not possible
        if (log.Level == 0)
        {
            _logger.LogTrace(template, log.Message, log.FrameUri);
        }
        else if (log.Level == 1)
        {
            _logger.LogWarning(template, log.Message, log.FrameUri);
        }
    }
}