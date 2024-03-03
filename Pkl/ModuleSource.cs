namespace Pkl;

public class ModuleSource 
{
    private static Uri _replTextUri = new("repl:text");
    public Uri Uri { get; set; } = default!;
    public string? Contents { get; set; }

    public static ModuleSource FileSource(params string[] pathParts)
    {
        var pathPartsCombined = Path.Combine(pathParts);
        var path = Path.GetFullPath(pathPartsCombined);
        var builer = new UriBuilder()
        {
            Scheme = Uri.UriSchemeFile,
            Path = path,
            Host = "" // Prevent "localohst" authority
        };

        return new ModuleSource
        {
            Uri = builer.Uri
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
        
        return UriSource(parsedUri);
    }

    public static ModuleSource UriSource(Uri uri)
    {
        return new ModuleSource
        {
            Uri = uri
        };
    }
}
