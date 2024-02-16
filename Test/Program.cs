using Pkl;
using Pkl.Evaluation;
using Pkl.EvaluatorManager;
using PklGenerator;

var manager = new EvaluatorManager([]);
var ver = manager.GetVersion();
Console.WriteLine(ver);
var evaluator = await manager.NewEvaluator(EvaluatorOptions.PreconfiguredOptons());
var response = await evaluator.EvaluateModule<Test>(ModuleSource.FileSource("Test.pkl"));
manager.Close();

var temp = new Test
{
    Host = "https://github.com",
    Port = 8080
};
