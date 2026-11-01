using Pkl.Evaluation;

namespace Pkl.Tests.Evaluation;

public class PklPropertyDecodingTests
{
    public class Mapped
    {
        public string Plain { get; set; } = default!;

        [PklProperty("my-prop")]
        public string MyProp { get; set; } = default!;

        // two Pkl properties that only differ in case: foo -> Foo, Foo -> Foo_
        public long Foo { get; set; }

        [PklProperty("Foo")]
        public long Foo_ { get; set; }

        [PklProperty("box")]
        public string Box_ { get; set; } = "unset";

        public string Unmapped { get; set; } = "stays";
    }

    [Fact]
    public async Task PropertiesAreMatchedByAttributeAndOtherwiseByUpperCasedName()
    {
        var manager = new EvaluatorManager.EvaluatorManager();
        try
        {
            var evaluator = await manager.NewEvaluator(EvaluatorOptions.PreconfiguredOptons());
            var result = await evaluator.EvaluateModule<Mapped>(ModuleSource.TextSource(
                """
                plain = "p"
                `my-prop` = "hyphen"
                foo = 1
                Foo = 2
                box = "boxed"
                somethingElse = "ignored"
                """));

            Assert.Equal("p", result.Plain);
            Assert.Equal("hyphen", result.MyProp);
            Assert.Equal(1, result.Foo);
            Assert.Equal(2, result.Foo_);
            Assert.Equal("boxed", result.Box_);
            Assert.Equal("stays", result.Unmapped);
        }
        finally
        {
            manager.Close();
        }
    }
}
