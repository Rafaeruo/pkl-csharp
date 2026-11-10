using Pkl.Evaluation;

namespace Pkl.Tests.Evaluation;

public class CommonTypeDecodingTests
{
    public class Links
    {
        public Uri Home { get; set; } = default!;
        public Uri? Backup { get; set; }
        public Uri? Missing { get; set; }
        public Uri[] Mirrors { get; set; } = default!;
        public Dictionary<Uri, string> Names { get; set; } = default!;
        public char Initial { get; set; }
        public char? MaybeInitial { get; set; }
        public char? NoInitial { get; set; }
        public char[] Letters { get; set; } = default!;
        public string StillAString { get; set; } = default!;
    }

    private const string Module = """
        typealias Secure = Uri(startsWith("https"))

        home: Uri = "https://example.com/a?b=c#d"
        backup: Secure? = "https://backup.example.com"
        missing: Uri? = null
        mirrors: Listing<Uri> = new { "https://a.example.com"; "package://example.com/p@1.0.0" }
        names: Mapping<Uri, String> = new { ["https://example.com"] = "main" }
        initial: Char = "x"
        maybeInitial: Char? = "é"
        noInitial: Char? = null
        letters: Listing<Char> = new { "a"; "b" }
        stillAString: Uri = "https://example.com"
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
    public async Task UrisAndCharsDecodeWhereverAValueCanAppear()
    {
        var links = await Evaluate<Links>(Module);

        Assert.Equal(new Uri("https://example.com/a?b=c#d"), links.Home);
        Assert.Equal("example.com", links.Home.Host);
        Assert.Equal(new Uri("https://backup.example.com"), links.Backup);
        Assert.Null(links.Missing);
        Assert.Equal(new[] { new Uri("https://a.example.com"), new Uri("package://example.com/p@1.0.0") }, links.Mirrors);
        Assert.Equal("main", links.Names[new Uri("https://example.com")]);
        Assert.Equal('x', links.Initial);
        Assert.Equal('é', links.MaybeInitial);
        Assert.Null(links.NoInitial);
        Assert.Equal(new[] { 'a', 'b' }, links.Letters);
    }

    public class NetLinks
    {
        public Uri Home { get; set; } = default!;
        public Uri? Missing { get; set; }
        public Uri Relative { get; set; } = default!;
        public Uri[] Mirrors { get; set; } = default!;
        public Dictionary<string, Uri> Names { get; set; } = default!;
        public string AsString { get; set; } = default!;
    }

    // pkl:net exists since Pkl 0.33
    [Fact]
    public async Task PklNetUrlsDecodeToUris()
    {
        const string module = """
            import "pkl:net"

            home: net.Url = net.Url("https://user@example.com:8080/a%20b?q=1#f")
            missing: net.Url? = null
            relative: net.Url = net.Url("../other/config.pkl")
            mirrors: Listing<net.Url> = new { net.Url("https://a.example.com"); net.Url("package://example.com/p@1.0.0") }
            names: Mapping<String, net.Url> = new { ["main"] = net.Url("https://example.com") }
            asString: net.Url = net.Url("https://example.com/s")
            """;

        NetLinks links;
        try
        {
            links = await Evaluate<NetLinks>(module);
        }
        catch (Exception e) when (e.Message.Contains("pkl:net") && !e.Message.Contains("isValidUrl"))
        {
            return; // this pkl is too old to have pkl:net
        }

        Assert.Equal(new Uri("https://user@example.com:8080/a%20b?q=1#f"), links.Home);
        Assert.Equal(8080, links.Home.Port);
        Assert.Null(links.Missing);
        Assert.False(links.Relative.IsAbsoluteUri);
        Assert.Equal("../other/config.pkl", links.Relative.OriginalString);
        Assert.Equal(new[] { new Uri("https://a.example.com"), new Uri("package://example.com/p@1.0.0") }, links.Mirrors);
        Assert.Equal(new Uri("https://example.com"), links.Names["main"]);
        Assert.Equal("https://example.com/s", links.AsString);
    }

    [Fact]
    public async Task UntypedUrisAndCharsStayStrings()
    {
        var result = await Evaluate<dynamic>("home = \"https://example.com\"\nletter = \"a\"");

        Assert.IsType<string>(result.Home);
        Assert.IsType<string>(result.Letter);
    }

    public class OneUri
    {
        public Uri Value { get; set; } = default!;
    }

    public class OneChar
    {
        public char Value { get; set; }
    }

    [Fact]
    public async Task StringsThatAreNotUrisGiveAClearError()
    {
        // a plain String is not checked by Pkl, so the conversion has to report it
        var ex = await Record.ExceptionAsync(() => Evaluate<OneUri>("value = \"http://\""));

        Assert.IsType<FormatException>(ex);
        Assert.Contains("\"http://\"", ex.Message);
        Assert.Contains("URI", ex.Message);
    }

    [Fact]
    public async Task StringsOfTheWrongLengthGiveAClearErrorForChar()
    {
        var ex = await Record.ExceptionAsync(() => Evaluate<OneChar>("value = \"ab\""));

        Assert.IsType<FormatException>(ex);
        Assert.Contains("\"ab\"", ex.Message);
        Assert.Contains("char", ex.Message);
    }
}
