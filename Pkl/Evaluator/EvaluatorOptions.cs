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
    public List<string> AllowedModules { get; set; }
    public List<string>? AllowedResources { get; set; }
    public List<IResourceReader>? ResourceReaders { get; set; }
    public List<IModuleReader>? ModuleReaders { get; set; }
    public string? CacheDir { get; set; }
    public string? RootDir { get; set; }
    public string? ProjectDir { get; set; }
    public ProjectDependencies? DeclaredProjectDependencies { get; set; }
    public ILogger Logger { get; set; } = NullLogger.Instance;

    public EvaluatorOptions()
    {
        // repl:text is the URI of the module used to hold expressions. It should always be allowed.
        AllowedModules = new List<string>() { "repl:text" };
    }

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

    public EvaluatorOptions WithFileSystemReader(string scheme)
    {
        var fsReader = new FileSystemReader(scheme);

        return WithResourceReader(fsReader)
            .WithModuleReader(fsReader);
    }

    public EvaluatorOptions WithResourceReader(IResourceReader resourceReader)
    {
        ResourceReaders ??= new List<IResourceReader>();
        ResourceReaders.Add(resourceReader);
        
        AllowedResources ??= new List<string>();
        AllowedResources.Add(resourceReader.Scheme);

        return this;
    }

    public EvaluatorOptions WithModuleReader(IModuleReader moduleReader)
    {
        ModuleReaders ??= new List<IModuleReader>();
        ModuleReaders.Add(moduleReader);

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
    public Dictionary<string, ProjectLocalDependency> LocalDependencies { get; set; } = default!;
    public Dictionary<string, ProjectRemoteDependency> RemoteDependencies { get; set; } = default!;
}

public class ProjectLocalDependency
{
    public string PackageUri { get; set; } = default!;
    public string ProjectFileUri { get; set; } = default!;
    public ProjectDependencies Dependencies { get; set; } = default!;
}

public class ProjectRemoteDependency
{
    public string PackageUri { get; set; } = default!;
    public Checksums Checksums { get; set; } = default!;
}

public class Checksums
{
    public string Sha256 { get; set; } = default!;
}