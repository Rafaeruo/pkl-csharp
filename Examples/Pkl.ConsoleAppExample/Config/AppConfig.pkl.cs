namespace Pkl.ConsoleAppExample.Config;

public class AppConfig
{
    public string Host { get; set; } = default!;

    public int Port { get; set; } = default!;

    public NestedConfig Nested { get; set; } = default!;
}
