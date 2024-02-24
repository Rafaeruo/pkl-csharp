namespace Pkl.Evaluation;

public interface IEvaluator
{
    Task<T> EvaluateModule<T>(ModuleSource source) where T : notnull;
    Task<string> EvaluateOutputText(ModuleSource source);
    Task<T> EvaluateOutputValue<T>(ModuleSource source) where T : notnull;
    Task<T> EvaluateExpression<T>(ModuleSource source, string? expr) where T : notnull;
    void Close();
}