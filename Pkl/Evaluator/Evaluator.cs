using Pkl.Decoding;
using Pkl.EvaluatorManager;
using Pkl.InternalMsgApi.Incoming;
using Pkl.InternalMsgApi.Outgoing;

namespace Pkl.Evaluator;

public class Evaluator : IEvaluator
{
    private readonly long _evaluatorId;
    private readonly IEvaluatorManager _evaluatorManager;
    private readonly Decoder _decoder;
    private readonly Dictionary<long, TaskCompletionSource<EvaluateResponse>> _pendingRequests = [];

    public Evaluator(long evaluatorId, IEvaluatorManager evaluatorManager, Decoder decoder)
    {
        _evaluatorId = evaluatorId;
        _evaluatorManager = evaluatorManager;
        _decoder = decoder;
    }

    public async Task<object> EvaluateExpression(ModuleSource source, string expr)
    {
        var raw = await EvaluateExpressionRaw(source, expr);
        return _decoder.Decode(raw);
    }

    public async Task<byte[]> EvaluateExpressionRaw(ModuleSource source, string expr)
    {
        var evaluate = new Evaluate
        {
            EvaluatorId = _evaluatorId,
            RequestId = 1, // TODO
            ModuleUri = source.Uri.ToString(),
            ModuleText = source.Contents,
            Expr = expr
        };

        var tcs = new TaskCompletionSource<EvaluateResponse>();
        _pendingRequests.Add(evaluate.RequestId, tcs);

        _evaluatorManager.Send(evaluate);

        var evaluateResponse = await tcs.Task;
        _pendingRequests.Remove(evaluate.RequestId);

        if (!string.IsNullOrEmpty(evaluateResponse.Error) || evaluateResponse.Result is null)
        {
            throw new Exception(evaluateResponse.Error);
        }

        return evaluateResponse.Result;
    }

    public void HandleEvaluateResponse(EvaluateResponse response)
    {
        if (!_pendingRequests.TryGetValue(response.RequestId, out var pending))
        {
            return;
        }

        pending.SetResult(response);
    }
}