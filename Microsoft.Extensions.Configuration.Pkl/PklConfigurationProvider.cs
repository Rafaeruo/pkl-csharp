using Pkl.Evaluation;
using Pkl.EvaluatorManager;

namespace Microsoft.Extensions.Configuration.Pkl;

internal sealed class PklConfigurationProvider : ConfigurationProvider
{
    private readonly PklConfigurationSource _source;
    private bool _shouldDisposeManager;
    private bool _shouldDisposeEvaluator;

    public PklConfigurationProvider(PklConfigurationSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _source = source;
    }

    public override void Load()
    {
        LoadEvaluator();
        var evaluationResult = _source.Evaluator!.EvaluateModule<object>(_source.ModuleSource).Result;
        DisposeEvaluator();

        Data = new Dictionary<string, string?>();
        if (evaluationResult is not IDictionary<string, object?> configuration)
        {
            return;
        }

        var parser = new PklConfigurationParser();
        Data = parser.Parse(configuration);
    }

    private void LoadEvaluator()
    {
        if (_source.Evaluator is null)
        {
            if (_source.Manager is null)
            {
                _shouldDisposeManager = true;
                _source.Manager = new EvaluatorManager();
            }

            _shouldDisposeEvaluator = true;
            var options = EvaluatorOptions.PreconfiguredOptons();
            _source.Evaluator = _source.Manager.NewEvaluator(options).Result;
        }
    }

    private void DisposeEvaluator()
    {
        if (_shouldDisposeManager)
        {
            _source.Manager!.Close();
        }
        else if (_shouldDisposeEvaluator)
        {
            _source.Evaluator!.Close();
        }
    }
}
