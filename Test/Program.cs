using Pkl.Evaluator;
using Pkl.EvaluatorManager;
using PklGenerator;

var evaluator = new EvaluatorManager([]);
var ver = evaluator.GetVersion();
Console.WriteLine(ver);
var _ = await evaluator.NewEvaluator(EvaluatorOptions.PreconfiguredOptons());
evaluator.Close();

var temp = new Test
{
    Host = "https://github.com",
    Port = "8080"
};
