namespace Pkl.Projects;

public class HttpSettings
{
    public ProxySettings? Proxy { get; set; }
}

public class ProxySettings
{
    public string? Address { get; set; }
    public string[]? NoProxy { get; set; }
}
