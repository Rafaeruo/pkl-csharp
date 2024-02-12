using Pkl.Evaluator;
using Pkl.EvaluatorManager;

var evaluator = new EvaluatorManager([]);
var ver = evaluator.GetVersion();
Console.WriteLine(ver);
var _ = await evaluator.NewEvaluator(EvaluatorOptions.PreconfiguredOptons());
evaluator.Close();