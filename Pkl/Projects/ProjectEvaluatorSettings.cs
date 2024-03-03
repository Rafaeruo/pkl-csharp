using Pkl.PklTypes;

namespace Pkl.Projects;

/// <summary>
/// C# representation of pkl.Project#Package.
/// </summary>
public class ProjectEvaluatorSettings
{
    public Dictionary<string, string>? ExternalProperties { get; set; }
    public Dictionary<string, string>? Env { get; set; }
    public string[]? AllowedModules { get; set; }
    public string[]? AllowedResources { get; set; }
    public bool NoCache { get; set; }
    public string[]? ModulePath { get; set; }
    public Duration? Timeout { get; set; }
    public string? ModuleCacheDir { get; set; }
    public string? RootDir { get; set; }
}
