namespace Pkl.Evaluation;

public interface IEvaluator
{
    Task<byte[]> EvaluateExpressionRaw(ModuleSource source, string expr);

    Task<object> EvaluateExpression(ModuleSource source, string expr); // TODO define a return type
}