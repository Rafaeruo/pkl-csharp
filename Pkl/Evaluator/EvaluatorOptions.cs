using System.Collections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Pkl.InternalMsgApi.Outgoing;
using Pkl.Projects;
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
    public Http? Http { get; set; }
    public ProjectDependencies? DeclaredProjectDependencies { get; set; }
    public ILogger Logger { get; set; } = NullLogger.Instance;
    public Dictionary<string, Type> TypeMappings = new();

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
            .WithDefaultCacheDir()
            .WithDefaultProjectTypeMappings();
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

    public EvaluatorOptions WithTypeMapping<T>(string pklTypeName)
    {
        TypeMappings[pklTypeName] = typeof(T);

        return this;
    }

    public EvaluatorOptions WithDefaultProjectTypeMappings()
    {
        // Needed for mapping ProjectDependencies
        return WithTypeMapping<Project>("pkl.Project")
            .WithTypeMapping<ProjectRemoteDependency>("pkl.Project#RemoteDependency");
    }

    public EvaluatorOptions WithProject(Project project)
    {
        return WithProjectEvaluatorSettings(project)
            .WithProjectDependencies(project);
    }

    public EvaluatorOptions WithProjectEvaluatorSettings(Project project)
    {   
        if (project.EvaluatorSettings is null)
        {
            return this;
        }
        
        Properties = project.EvaluatorSettings.ExternalProperties;
        CacheDir = project.EvaluatorSettings.NoCache ? "" : project.EvaluatorSettings.ModuleCacheDir;
        RootDir = project.EvaluatorSettings.RootDir;

        if (project.EvaluatorSettings.Http?.Proxy != null)
        {
            Http = new Http
            {
                Proxy = new Proxy
                {
                    Address = project.EvaluatorSettings.Http.Proxy.Address,
                    NoProxy = project.EvaluatorSettings.Http.Proxy.NoProxy
                }
            };
        }

        if (project.EvaluatorSettings.AllowedModules?.Length > 0)
        {
            AllowedModules.AddRange(project.EvaluatorSettings.AllowedModules);
        }

        if (project.EvaluatorSettings.AllowedResources?.Length > 0)
        {
            AllowedModules ??= new List<string>(project.EvaluatorSettings.AllowedResources.Length);
            AllowedModules.AddRange(project.EvaluatorSettings.AllowedResources);
        }

        if (project.EvaluatorSettings.Env?.Count > 0)
        {
            if (Env is null)
            {
                Env = project.EvaluatorSettings.Env;
            }
            else
            {
                foreach (var kvp in project.EvaluatorSettings.Env)
                {
                    Env[kvp.Key] = kvp.Value;
                }
            }
        }

        return this;
    }

    public EvaluatorOptions WithProjectDependencies(Project project)
    {
        const string prefix = "file://";
        const string suffix = "/PklProject";

        var projectDir = project.ProjectFileUri;
        if (projectDir.StartsWith(prefix))
        {
            projectDir = projectDir.Substring(prefix.Length); // Trim start
        }

        if (projectDir.EndsWith(suffix))
        {
            projectDir = projectDir[..^suffix.Length]; // Trim end
        }

        ProjectDir = projectDir;
        DeclaredProjectDependencies = project.ProjectDependencies;
        return this;
    }
}

public class ProjectDependencies
{
    public Dictionary<string, ProjectLocalDependency> LocalDependencies { get; set; } = default!;
    public Dictionary<string, ProjectRemoteDependency> RemoteDependencies { get; set; } = default!;

    internal Dictionary<string, InternalMsgApi.Outgoing.ProjectOrDependency> ToMsgPack()
    {
        var count = LocalDependencies.Count + RemoteDependencies.Count;
        var returnValue = new Dictionary<string, InternalMsgApi.Outgoing.ProjectOrDependency>(count);
        
        foreach (var localDependency in LocalDependencies)
        {
            returnValue[localDependency.Key] = localDependency.Value.ToMsgPack();
        }

        foreach (var remoteDependency in RemoteDependencies)
        {
            returnValue[remoteDependency.Key] = remoteDependency.Value.ToMsgPack();
        }

        return returnValue;
    }
}

public class ProjectLocalDependency
{
    public string PackageUri { get; set; } = default!;
    public string ProjectFileUri { get; set; } = default!;
    public ProjectDependencies? Dependencies { get; set; }

    internal InternalMsgApi.Outgoing.ProjectOrDependency ToMsgPack()
    {
        return new InternalMsgApi.Outgoing.ProjectOrDependency
        {
            PackageUri = PackageUri,
            ProjectFileUri = ProjectFileUri,
            Type = "local",
            Dependencies = Dependencies?.ToMsgPack()
        };
    }
}

public class ProjectRemoteDependency
{
    public string Uri { get; set; } = default!;
    public Checksums? Checksums { get; set; }

    internal InternalMsgApi.Outgoing.ProjectOrDependency ToMsgPack()
    {
        return new InternalMsgApi.Outgoing.ProjectOrDependency
        {
            PackageUri = Uri,
            Checksums = Checksums?.ToMsgPack(),
            Type = "remote"
        };
    }
}

public class Checksums
{
    public string Sha256 { get; set; } = default!;

    internal InternalMsgApi.Outgoing.Checksums? ToMsgPack()
    {
        return new InternalMsgApi.Outgoing.Checksums
        {
            Sha256 = Sha256
        };
    }
}
