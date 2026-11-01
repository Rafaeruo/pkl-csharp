using System.Globalization;
using System.Net;
using Pkl.Evaluation;

namespace Pkl.Tests.Evaluation;

public class ParsableTypeDecodingTests
{
    /// <summary>A user type that implements IParsable explicitly, with no public TryParse.</summary>
    public readonly record struct Money(decimal Amount, string Currency) : IParsable<Money>
    {
        static Money IParsable<Money>.Parse(string s, IFormatProvider? provider) =>
            TryParseCore(s, out var money) ? money : throw new FormatException(s);

        static bool IParsable<Money>.TryParse(string? s, IFormatProvider? provider, out Money result) =>
            TryParseCore(s, out result);

        private static bool TryParseCore(string? s, out Money result)
        {
            var parts = s?.Split(' ');
            if (parts is { Length: 2 } && decimal.TryParse(parts[0], NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
            {
                result = new Money(amount, parts[1]);
                return true;
            }

            result = default;
            return false;
        }
    }

    public class Things
    {
        public Guid Id { get; set; }
        public Guid? NoId { get; set; }
        public Guid[] Ids { get; set; } = default!;
        public Dictionary<Guid, string> ById { get; set; } = default!;
        public DateTime Utc { get; set; }
        public DateTime Unspecified { get; set; }
        public DateTimeOffset Offset { get; set; }
        public DateOnly Day { get; set; }
        public TimeOnly Time { get; set; }
        public TimeSpan Span { get; set; }
        public Version Version { get; set; } = default!;
        public IPAddress Address { get; set; } = default!;
        public Money Price { get; set; }
        public Money? NoPrice { get; set; }
    }

    private const string Module = """
        id: String = "3f2504e0-4f89-11d3-9a0c-0305e82c3301"
        noId: String? = null
        ids: Listing<String> = new { "3f2504e0-4f89-11d3-9a0c-0305e82c3301"; "00000000-0000-0000-0000-000000000001" }
        byId: Mapping<String, String> = new { ["3f2504e0-4f89-11d3-9a0c-0305e82c3301"] = "first" }
        utc: String = "2024-03-01T10:30:00Z"
        unspecified: String = "2024-03-01T10:30:00"
        offset: String = "2024-03-01T10:30:00+02:00"
        day: String = "2024-03-01"
        time: String = "10:30:15"
        span: String = "1.02:03:04"
        version: String = "1.2.3"
        address: String = "192.168.0.1"
        price: String = "12.50 EUR"
        noPrice: String? = null
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
    public async Task StringsDecodeIntoParsableTypes()
    {
        var things = await Evaluate<Things>(Module);

        var guid = Guid.Parse("3f2504e0-4f89-11d3-9a0c-0305e82c3301");
        Assert.Equal(guid, things.Id);
        Assert.Null(things.NoId);
        Assert.Equal(new[] { guid, Guid.Parse("00000000-0000-0000-0000-000000000001") }, things.Ids);
        Assert.Equal("first", things.ById[guid]);

        Assert.Equal(new DateTime(2024, 3, 1, 10, 30, 0, DateTimeKind.Utc), things.Utc);
        Assert.Equal(DateTimeKind.Utc, things.Utc.Kind);
        Assert.Equal(DateTimeKind.Unspecified, things.Unspecified.Kind);
        Assert.Equal(new DateTimeOffset(2024, 3, 1, 10, 30, 0, TimeSpan.FromHours(2)), things.Offset);
        Assert.Equal(new DateOnly(2024, 3, 1), things.Day);
        Assert.Equal(new TimeOnly(10, 30, 15), things.Time);
        Assert.Equal(new TimeSpan(1, 2, 3, 4), things.Span);
        Assert.Equal(new Version(1, 2, 3), things.Version);
        Assert.Equal(IPAddress.Parse("192.168.0.1"), things.Address);
        Assert.Equal(new Money(12.50m, "EUR"), things.Price);
        Assert.Null(things.NoPrice);
    }

    public class OneGuid
    {
        public Guid Value { get; set; }
    }

    public class OneInt
    {
        public int Value { get; set; }
    }

    [Fact]
    public async Task StringsThatDoNotParseGiveAClearError()
    {
        var ex = await Record.ExceptionAsync(() => Evaluate<OneGuid>("value = \"not a guid\""));

        Assert.IsType<FormatException>(ex);
        Assert.Contains("\"not a guid\"", ex.Message);
        Assert.Contains("Guid", ex.Message);
    }

    [Fact]
    public async Task StringsAreNotSilentlyAcceptedForNumbers()
    {
        var ex = await Record.ExceptionAsync(() => Evaluate<OneInt>("value = \"5\""));

        Assert.NotNull(ex);
    }

    [Fact]
    public async Task UntypedStringsStayStrings()
    {
        var result = await Evaluate<dynamic>("id = \"3f2504e0-4f89-11d3-9a0c-0305e82c3301\"");

        Assert.IsType<string>(result.Id);
    }
}
