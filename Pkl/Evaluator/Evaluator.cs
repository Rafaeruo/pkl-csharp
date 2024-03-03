using Microsoft.Extensions.Logging;
using Pkl.Decoding;
using Pkl.EvaluatorManager;
using Pkl.InternalMsgApi.Incoming;
using Pkl.InternalMsgApi.Outgoing;
using Pkl.Projects;

namespace Pkl.Evaluation;

public class Evaluator : IEvaluator
{
    private readonly long _evaluatorId;
    private readonly IEvaluatorManager _evaluatorManager;
    private readonly Decoder _decoder;
    private bool _closed;
    private readonly ILogger _logger;
    private readonly ICollection<Reader.IResourceReader> _resourceReaders = Array.Empty<Reader.IResourceReader>();
    private readonly ICollection<Reader.IModuleReader> _moduleReaders = Array.Empty<Reader.IModuleReader>();

    public Evaluator(
        long evaluatorId, 
        IEvaluatorManager evaluatorManager, 
        Decoder decoder, 
        EvaluatorOptions options)
    {
        _evaluatorId = evaluatorId;
        _evaluatorManager = evaluatorManager;
        _decoder = decoder;
        _logger = options.Logger;

        if (options.ResourceReaders is not null)
        {
            _resourceReaders = options.ResourceReaders;
        }

        if (options.ModuleReaders is not null)
        {
            _moduleReaders = options.ModuleReaders;
        }
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

    internal void HandleLog(Log log)
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

    internal void HandleReadResource(ReadResource readResource)
    {
        var response = new ReadResourceResponse()
        {
            RequestId = readResource.RequestId,
            EvaluatorId = _evaluatorId
        };

        var uri = new Uri(readResource.Uri);
        var reader = _resourceReaders.FirstOrDefault(r => r.Scheme == uri.Scheme);
        
        if (reader is null)
        {
            response.Error = $"No resource reader found for scheme {uri.Scheme}";
            _evaluatorManager.Send(response);
            return;
        }
               
        response.Contents = reader.Read(uri);
        _evaluatorManager.Send(response);
    }

    internal void HandleReadModule(ReadModule readModule)
    {
        var response = new ReadModuleResponse()
        {
            RequestId = readModule.RequestId,
            EvaluatorId = _evaluatorId
        };

        var uri = new Uri(readModule.Uri);
        var reader = _moduleReaders.FirstOrDefault(r => r.Scheme == uri.Scheme);
        
        if (reader is null)
        {
            response.Error = $"No module reader found for scheme {uri.Scheme}";
            _evaluatorManager.Send(response);
            return;
        }
               
        response.Contents = reader.Read(uri);
        _evaluatorManager.Send(response);
    }

    internal void HandleListResources(ListResources listResources)
    {
        var response = new ListResourcesResponse()
        {
            RequestId = listResources.RequestId,
            EvaluatorId = _evaluatorId
        };

        var uri = new Uri(listResources.Uri);
        var reader = _resourceReaders.FirstOrDefault(r => r.Scheme == uri.Scheme);
        
        if (reader is null)
        {
            response.Error = $"No resource reader found for scheme {uri.Scheme}";
            _evaluatorManager.Send(response);
            return;
        }
               
        var pathElements = reader.ListElements(uri);

        response.PathElements = new List<PathElement>(pathElements.Length);
        foreach (var pathElement in pathElements)
        {
            response.PathElements.Add(new PathElement
            {
                Name = pathElement.Name,
                IsDirectory = pathElement.IsDirectory
            });
        }

        _evaluatorManager.Send(response);
    }

    internal void HandleListModules(ListModules listModules)
    {
        var response = new ListModulesResponse()
        {
            RequestId = listModules.RequestId,
            EvaluatorId = _evaluatorId
        };

        var uri = new Uri(listModules.Uri);
        var reader = _moduleReaders.FirstOrDefault(r => r.Scheme == uri.Scheme);
        
        if (reader is null)
        {
            response.Error = $"No module reader found for scheme {uri.Scheme}";
            _evaluatorManager.Send(response);
            return;
        }
               
        var pathElements = reader.ListElements(uri);

        response.PathElements = new List<PathElement>(pathElements.Length);
        foreach (var pathElement in pathElements)
        {
            response.PathElements.Add(new PathElement
            {
                Name = pathElement.Name,
                IsDirectory = pathElement.IsDirectory
            });
        }

        _evaluatorManager.Send(response);
    }

    public async Task<Project> LoadProject(string path)
    {
        return await EvaluateOutputValue<Project>(ModuleSource.FileSource(path));
    }
}
