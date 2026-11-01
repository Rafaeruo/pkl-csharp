using Pkl.Evaluation;

namespace Pkl.Tests.Evaluation;

public class StringUnionDecodingTests
{
    [PklStringUnion]
    public enum Status
    {
        [PklStringUnionValue("active")]
        Active,

        // no attribute: matches the Pkl string "Done" exactly
        Done,

        [PklStringUnionValue("in-progress")]
        InProgress,

        [PklStringUnionValue("1st place")]
        FirstPlace,
    }

    // not marked [PklStringUnion]
    public enum Plain
    {
        Active,
    }

    public class Job
    {
        public Status Current { get; set; }
        public Status? Optional { get; set; }
        public Status? Missing { get; set; }
        public Status[] History { get; set; } = default!;
        public Dictionary<Status, string> Labels { get; set; } = default!;
        public HashSet<Status> Seen { get; set; } = default!;
        public string StillAString { get; set; } = default!;
    }

    private const string Module = """
        typealias Status = "active" | "Done" | "in-progress" | "1st place"

        current: Status = "in-progress"
        optional: Status? = "1st place"
        missing: Status? = null
        history: Listing<Status> = new { "active"; "in-progress"; "Done" }
        labels: Mapping<Status, String> = new { ["active"] = "A"; ["1st place"] = "F" }
        seen: Set<Status> = Set("active", "in-progress")
        stillAString: String = "in-progress"
        """;

    private static async Task<T> Evaluate<T>(string pkl) where T : notnull
    {
        var manager = new EvaluatorManager.EvaluatorManager();
        try
        {
            var evaluator = await manager.NewEvaluator(EvaluatorOptions.PreconfiguredOptons());
            return await evaluator.EvaluateModule<T>(ModuleSource.TextSource(pkl));
        }
        finally
        {
            manager.Close();
        }
    }

    [Fact]
    public async Task StringUnionsDecodeIntoMarkedEnumsEverywhereAValueCanAppear()
    {
        var job = await Evaluate<Job>(Module);

        Assert.Equal(Status.InProgress, job.Current);
        Assert.Equal(Status.FirstPlace, job.Optional);
        Assert.Null(job.Missing);
        Assert.Equal(new[] { Status.Active, Status.InProgress, Status.Done }, job.History);
        Assert.Equal(new Dictionary<Status, string> { [Status.Active] = "A", [Status.FirstPlace] = "F" }, job.Labels);
        Assert.Equal(new HashSet<Status> { Status.Active, Status.InProgress }, job.Seen);
        Assert.Equal("in-progress", job.StillAString);
    }

    public class OneStatus
    {
        public Status Current { get; set; }
    }

    [Fact]
    public async Task ValuesOutsideTheEnumGiveAClearError()
    {
        var ex = await Record.ExceptionAsync(() => Evaluate<OneStatus>("current = \"finished\""));

        Assert.IsType<FormatException>(ex);
        Assert.Contains("\"finished\"", ex.Message);
        Assert.Contains("Status", ex.Message);
        Assert.Contains("\"in-progress\"", ex.Message);
    }

    public class OnePlain
    {
        public Plain Current { get; set; }
    }

    [Fact]
    public async Task EnumsWithoutTheMarkerAreRejectedWithGuidance()
    {
        var ex = await Record.ExceptionAsync(() => Evaluate<OnePlain>("current = \"Active\""));

        Assert.IsType<NotSupportedException>(ex);
        Assert.Contains("[PklStringUnion]", ex.Message);
    }

    [Fact]
    public async Task UntypedStringsStayStrings()
    {
        var result = await Evaluate<dynamic>("status = \"in-progress\"");

        Assert.Equal("in-progress", result.Status);
    }
}
