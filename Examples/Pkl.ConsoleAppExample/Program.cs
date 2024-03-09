using Pkl;
using Pkl.ConsoleAppExample.Config;
using Pkl.Evaluation;
using Pkl.EvaluatorManager;

var manager = new EvaluatorManager();
var evaluator = await manager.NewEvaluator(EvaluatorOptions.PreconfiguredOptons());

var moduleSource = ModuleSource.FileSource("Config/Pkl/config.pkl");
var response = await evaluator.EvaluateModule<AppConfig>(moduleSource);

Console.WriteLine(response.Host);
Console.WriteLine(response.Port);
Console.WriteLine(response.Nested.Title);

manager.Close();
