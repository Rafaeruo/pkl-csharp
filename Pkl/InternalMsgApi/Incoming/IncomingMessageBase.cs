using MessagePack;

namespace Pkl.InternalMsgApi.Incoming;

[Union((int)Code.CodeNewEvaluatorResponse, typeof(CreateEvaluatorResponse))]
[Union((int)Code.CodeEvaluateResponse, typeof(EvaluateResponse))]
[Union((int)Code.CodeEvaluateRead, typeof(ReadResource))]
[Union((int)Code.CodeListResourcesRequest, typeof(ListResources))]
[Union((int)Code.CodeEvaluateReadModule, typeof(ReadModule))]
[Union((int)Code.CodeListModulesRequest, typeof(ListModules))]
[Union((int)Code.CodeEvaluateLog, typeof(Log))]
public abstract class IncomingMessageBase 
{
    protected abstract Code Code { get; set; }
}