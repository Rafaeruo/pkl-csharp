using MessagePack;
using Pkl.Evaluation;

namespace Pkl.InternalMsgApi.Outgoing;

[MessagePackObject]
[MessagePackFormatter(typeof(NoDefaultsFormatter<CreateEvaluator>))]
public class CreateEvaluator : OutgoingMessageBase
{
    internal CreateEvaluator(EvaluatorOptions options)
    {
        RequestId = 1; // TODO
        AllowedModules = options.AllowedModules;
        AllowedResources = options.AllowedResources;
        CacheDir = options.CacheDir;
        Env = options.Env;
        ModulePaths = options.ModulePaths;
        RootDir = options.RootDir;
        OutputFormat = options.OutputFormat;
        Properties = options.Properties;

        ClientModuleReaders = options.ModuleReaders
            ?.Select(rr => new ModuleReader
            {
                Scheme = rr.Scheme,
                HasHierarchicalUris = rr.HasHierarchicalUris,
                IsGlobbable = rr.IsGlobbable,
                IsLocal = rr.IsLocal
            }).ToArray();

        ClientResourceReaders = options.ModuleReaders
            ?.Select(mr => new ResourceReader
            {
                Scheme = mr.Scheme,
                HasHierarchicalUris = mr.HasHierarchicalUris,
                IsGlobbable = mr.IsGlobbable
            }).ToArray();

        if (!string.IsNullOrEmpty(options.ProjectDir))
        {
            Project = new ProjectOrDependency
            {
                ProjectFileUri = $"file://{options.ProjectDir}/PklProject",
                Dependencies = options.DeclaredProjectDependencies?.ToMsgPack()
            };
        }
    }

    [Key("requestId")]
    public long RequestId { get; set; }

    [Key("clientResourceReaders")]
    public ICollection<ResourceReader>? ClientResourceReaders { get; set; }

    [Key("clientModuleReaders")]
    public ICollection<ModuleReader>? ClientModuleReaders { get; set; }

    [Key("modulePaths")]
    public ICollection<string>? ModulePaths { get; set; }

    [Key("env")]
    public Dictionary<string, string>? Env { get; set; }

    [Key("properties")]
    public Dictionary<string, string>? Properties { get; set; }

    [Key("outputFormat")]
    public string? OutputFormat { get; set; }

    [Key("allowedModules")]
    public ICollection<string>? AllowedModules { get; set; }

    [Key("allowedResources")]
    public ICollection<string>? AllowedResources { get; set; }

    [Key("rootDir")]
    public string? RootDir { get; set; }

    [Key("cacheDir")]
    public string? CacheDir { get; set; }

    [Key("project")]
    public ProjectOrDependency? Project { get; set; }

    protected override Code Code { get; set; } = Code.CodeNewEvaluator;
}

[MessagePackObject]
public class ResourceReader
{
    [Key("scheme")]
    public string Scheme { get; set; } = default!;

    [Key("hasHierarchicalUris")]
    public bool HasHierarchicalUris { get; set; }

    [Key("isGlobbable")]
    public bool IsGlobbable { get; set; }
}

[MessagePackObject]
public class ModuleReader
{
    [Key("scheme")]
    public string Scheme { get; set; } = default!;

    [Key("hasHierarchicalUris")]
    public bool HasHierarchicalUris { get; set; }

    [Key("isGlobbable")]
    public bool IsGlobbable { get; set; }

    [Key("isLocal")]
    public bool IsLocal { get; set; }
}

[MessagePackObject]
public class Checksums
{
    [Key("sha256")]
    public string Sha256 { get; set; } = default!;
}

[MessagePackObject]
[MessagePackFormatter(typeof(NoDefaultsFormatter<ProjectOrDependency>))]
public class ProjectOrDependency
{
    [Key("packageUri")]
    public string? PackageUri { get; set; }

    [Key("type")]
    public string Type { get; set; } = default!;

    [Key("projectFileUri")]
    public string? ProjectFileUri { get; set; }

    [Key("checksums")]
    public Checksums? Checksums { get; set; }

    [Key("dependencies")]
    public Dictionary<string, ProjectOrDependency>? Dependencies { get; set; }
}
