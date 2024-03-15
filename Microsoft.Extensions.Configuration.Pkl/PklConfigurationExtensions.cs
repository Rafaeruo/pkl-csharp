using Pkl;
using Pkl.Evaluation;
using Pkl.EvaluatorManager;

namespace Microsoft.Extensions.Configuration.Pkl;

public static class PklConfigurationExtensions
{
    public static IConfigurationBuilder AddPklModule(
        this IConfigurationBuilder builder, 
        ModuleSource moduleSource,
        IEvaluatorManager? manager = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(moduleSource);

        builder.Add(new PklConfigurationSource(moduleSource, manager));

        return builder;
    }

    public static IConfigurationBuilder AddPklModule(
        this IConfigurationBuilder builder,
        ModuleSource moduleSource,
        IEvaluator evaluator)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(moduleSource);
        ArgumentNullException.ThrowIfNull(evaluator);

        builder.Add(new PklConfigurationSource(moduleSource, evaluator));

        return builder;
    }
}
