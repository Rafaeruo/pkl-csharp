using Pkl.Evaluation;

namespace Pkl.Tests.Evaluation;

public class ConcurrentManagersTests
{
    public class Result
    {
        public long Id { get; set; }
        public string Text { get; set; } = default!;
    }

    /// <summary>
    /// Several managers (each with its own pkl process) evaluating at once. Their stdout readers share
    /// pooled buffers, so a message that is decoded after the next read has started can be overwritten
    /// by another reader, which showed up as evaluator IDs the server had never issued, or as hangs.
    /// </summary>
    [Fact]
    public async Task ManagersEvaluatingConcurrentlyDoNotSeeEachOthersMessages()
    {
        const int managers = 6;
        const int evaluationsEach = 25;

        var workers = Enumerable.Range(0, managers).Select(m => Task.Run(async () =>
        {
            var manager = new EvaluatorManager.EvaluatorManager();
            try
            {
                var evaluator = await manager.NewEvaluator(EvaluatorOptions.PreconfiguredOptons());
                for (var i = 0; i < evaluationsEach; i++)
                {
                    var id = m * 1000 + i;
                    var result = await evaluator.EvaluateModule<Result>(
                        ModuleSource.TextSource($"id = {id}\ntext = \"manager {m} evaluation {i}\""));

                    Assert.Equal(id, result.Id);
                    Assert.Equal($"manager {m} evaluation {i}", result.Text);
                }
            }
            finally
            {
                manager.Close();
            }
        })).ToArray();

        var all = Task.WhenAll(workers);
        var finished = await Task.WhenAny(all, Task.Delay(TimeSpan.FromSeconds(45), TestContext.Current.CancellationToken));
        Assert.True(finished == all, "evaluations did not complete in time (a response was likely lost)");
        await all;
    }
}
