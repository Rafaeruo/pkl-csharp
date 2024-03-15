using Pkl;
using Pkl.Evaluation;
using Pkl.EvaluatorManager;

namespace Microsoft.Extensions.Configuration.Pkl;

public sealed class PklConfigurationSource : IConfigurationSource
{
    public ModuleSource ModuleSource { get; set; }
    public IEvaluator? Evaluator { get; set; }
    public IEvaluatorManager? Manager { get; set; }

    public PklConfigurationSource(ModuleSource moduleSource, IEvaluatorManager? manager = null)
    {
        ArgumentNullException.ThrowIfNull(moduleSource);

        ModuleSource = moduleSource;
        Manager = manager;
    }

    public PklConfigurationSource(ModuleSource moduleSource, IEvaluator evaluator)
    {
        ArgumentNullException.ThrowIfNull(moduleSource);
        ArgumentNullException.ThrowIfNull(evaluator);

        ModuleSource = moduleSource;
        Evaluator = evaluator;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new PklConfigurationProvider(this);
    }
}
