using System.Collections;

namespace Pkl.Evaluation;

public class EvaluatorOptions
{
    public Dictionary<string, string>? Properties { get; set; }
    public Dictionary<string, string>? Env { get; set; }
    public List<string>? ModulePaths { get; set; }
    public string? OutputFormat { get; set; }
    public List<string> AllowedModules { get; set; } = [];
    public List<string> AllowedResources { get; set; } = [];
    public List<Reader.IResourceReader>? ResourceReaders { get; set; }
    public List<Reader.IModuleReader>? ModuleReaders { get; set; }
    public string? CacheDir { get; set; }
    public string? RootDir { get; set; }
    public string? ProjectDir { get; set; }
    public ProjectDependencies? DeclaredProjectDependencies { get; set; }

    public static EvaluatorOptions PreconfiguredOptons()
    {
        var environmentDict = new Dictionary<string, string>();
        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            if (entry.Value is null)
            {
                continue;
            }
            
            environmentDict.Add(entry.Key.ToString()!, entry.Value.ToString()!);
        }

        var cacheDir = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".pkl/cache");

        return new EvaluatorOptions
        {
            AllowedResources = ["http:", "https:", "file:", "env:", "prop:", "modulepath:", "package:", "projectpackage:"],
            AllowedModules = ["pkl:", "repl:", "file:", "http:", "https:", "modulepath:", "package:", "projectpackage:"],
            Env = environmentDict,
            CacheDir = cacheDir,
        };
    }
}

public class ProjectDependencies
{
    public Dictionary<string, ProjectLocalDependency> LocalDependencies { get; set; } = [];
    public Dictionary<string, ProjectRemoteDependency> RemoteDependencies { get; set; } = [];
}

public class ProjectLocalDependency
{
    public required string PackageUri { get; set; }
    public required string ProjectFileUri { get; set; }
    public required ProjectDependencies Dependencies { get; set; }
}

public class ProjectRemoteDependency
{
    public required string PackageUri { get; set; }
    public required Checksums Checksums { get; set; }
}

public class Checksums
{
    public required string Sha256 { get; set; }
}