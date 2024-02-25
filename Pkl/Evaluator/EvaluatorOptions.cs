using System.Collections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Pkl.Reader;

namespace Pkl.Evaluation;

public class EvaluatorOptions
{
    public Dictionary<string, string>? Properties { get; set; }
    public Dictionary<string, string>? Env { get; set; }
    public List<string>? ModulePaths { get; set; }
    public string? OutputFormat { get; set; }
    public List<string> AllowedModules { get; set; } = [];
    public List<string> AllowedResources { get; set; } = [];
    public List<IResourceReader>? ResourceReaders { get; set; }
    public List<IModuleReader>? ModuleReaders { get; set; }
    public string? CacheDir { get; set; }
    public string? RootDir { get; set; }
    public string? ProjectDir { get; set; }
    public ProjectDependencies? DeclaredProjectDependencies { get; set; }
    public ILogger Logger { get; set; } = NullLogger.Instance;

    public static EvaluatorOptions PreconfiguredOptons()
    {
        return new EvaluatorOptions()
            .WithDefaultAllowedResources()
            .WithDefaultAllowedModules()
            .WithOsEnv()
            .WithDefaultCacheDir();
    }

    public EvaluatorOptions WithDefaultAllowedResources()
    {
        AllowedResources ??= new List<string>();
        var defaultAllowedResources = new string[] 
        { 
            "http:", "https:", "file:", "env:", "prop:", "modulepath:", "package:", "projectpackage:" 
        };

        AllowedResources.AddRange(defaultAllowedResources);

        return this;
    }

    public EvaluatorOptions WithDefaultAllowedModules()
    {
        AllowedModules ??= new List<string>();
        var defaultAllowedModules = new string[] 
        { 
            "pkl:", "repl:", "file:", "http:", "https:", "modulepath:", "package:", "projectpackage:"
        };

        AllowedModules.AddRange(defaultAllowedModules);

        return this;
    }

    public EvaluatorOptions WithOsEnv()
    {
        Env ??= new Dictionary<string, string>();

        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            if (entry.Value is null)
            {
                continue;
            }
            
            Env[entry.Key.ToString()!] = entry.Value.ToString()!;
        }

        return this;
    }

    public EvaluatorOptions WithDefaultCacheDir()
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        const string defaultPklCacheDir = ".pkl/cache";

        CacheDir = Path.Join(homeDirectory, defaultPklCacheDir);

        return this;
    }

    public EvaluatorOptions WithResourceReader(Reader.IResourceReader resourceReader)
    {
        ResourceReaders ??= new List<Reader.IResourceReader>();
        ResourceReaders.Add(resourceReader);
        
        AllowedResources ??= new List<string>();
        AllowedResources.Add(resourceReader.Scheme);

        return this;
    }

    public EvaluatorOptions WithModuleReader(Reader.IModuleReader moduleReader)
    {
        ModuleReaders ??= new List<Reader.IModuleReader>();
        ModuleReaders.Add(moduleReader);

        AllowedModules ??= new List<string>();
        AllowedModules.Add(moduleReader.Scheme); 

        return this;
    }

    public EvaluatorOptions WithLogger(ILogger logger)
    {
        Logger = logger;

        return this;
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