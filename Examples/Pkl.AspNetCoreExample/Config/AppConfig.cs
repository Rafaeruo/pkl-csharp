namespace Pkl.AspNetCoreExample.Config;

public class AppConfig
{
    public string Text { get; set; } = default!;
    public int Port { get; set; }
    public Dictionary<string, int> Mapping { get; set; }  = default!;
    public int[] Listing { get; set; } = default!;
}
