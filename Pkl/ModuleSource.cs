namespace Pkl;

public class ModuleSource 
{
    private static Uri _replTextUri = new("repl:text");
    public required Uri Uri { get; set; }
    public string? Contents { get; set; }

    public static ModuleSource FileSource(params string[] pathParts)
    {
        var pathPartsCombined = Path.Combine(pathParts);
        var uri = new Uri($"file://{pathPartsCombined}");

        return new ModuleSource
        {
            Uri = uri
        };
    }

    public static ModuleSource TextSource(string text)
    {
        return new ModuleSource
        {
            Uri = _replTextUri,
            Contents = text
        };
    }

    public static ModuleSource UriSource(string uri)
    {
        var parsedUri = new Uri(uri);

        return new ModuleSource
        {
            Uri = parsedUri
        };
    }

    public static ModuleSource UriSource(Uri uri)
    {
        return new ModuleSource
        {
            Uri = uri
        };
    }
}