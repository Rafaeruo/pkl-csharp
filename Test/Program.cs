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

var thing1 = GeneratedEnum.TestEnumMem;
var thing2 = new GeneratedClass();
thing2.EnumVal = thing1;
