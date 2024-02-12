namespace Pkl.InternalMsgApi;

public enum Code {
	CodeNewEvaluator = 0x20,
	CodeNewEvaluatorResponse = 0x21,
	CodeCloseEvaluator = 0x22,
	CodeEvaluate = 0x23,
	CodeEvaluateResponse = 0x24,
	CodeEvaluateLog = 0x25,
	CodeEvaluateRead = 0x26,
	CodeEvaluateReadResponse = 0x27,
	CodeEvaluateReadModule = 0x28,
	CodeEvaluateReadModuleResponse = 0x29,
	CodeListResourcesRequest = 0x2a,
	CodeListResourcesResponse = 0x2b,
	CodeListModulesRequest = 0x2c,
	CodeListModulesResponse = 0x2d
}