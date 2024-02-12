using MessagePack;

namespace Pkl.InternalMsgApi.Outgoing;

[MessagePackObject]
[MessagePackFormatter(typeof(NoDefaultsFormatter<CreateEvaluator>))]
public class CreateEvaluator : OutgoingMessageBase 
{
    [Key("requestId")]
    public long RequestId { get; set; }

    [Key("clientResourceReaders")]
    public ResourceReader[]? ClientResourceReaders { get; set; }

    [Key("clientModuleReaders")]
    public ModuleReader[]? ClientModuleReaders { get; set; }

    [Key("modulePaths")]
    public string[]? ModulePaths { get; set; }

    [Key("env")]
    public Dictionary<string, string>? Env { get; set; }

    [Key("properties")]
    public Dictionary<string, string>? Properties { get; set; }

    [Key("outputFormat")]
    public string? OutputFormat { get; set; }

    [Key("allowedModules")]
    public string[]? AllowedModules { get; set; }

    [Key("allowedResources")]
    public string[]? AllowedResources { get; set; }

    [Key("rootDir")]
    public string? RootDir { get; set; }

    [Key("cacheDir")]
    public string? CacheDir { get; set; }

    [Key("project")]
    public ProjectOrDependency? Project { get; set; }

    protected override Code Code { get; set; } = Code.CodeNewEvaluator;
}

[MessagePackObject(keyAsPropertyName: true)]
public class ResourceReader
{
    [Key("scheme")]
    public required string Scheme { get; set; }

    [Key("hasHierarchicalUris")]
    public bool HasHierarchicalUris { get; set; }

    [Key("isGlobbable")]
    public bool IsGlobbable { get; set; }
}

[MessagePackObject(keyAsPropertyName: true)]
public class ModuleReader
{
    [Key("scheme")]
    public required string Scheme { get; set; }

    [Key("hasHierarchicalUris")]
    public bool HasHierarchicalUris { get; set; }
}

[MessagePackObject(keyAsPropertyName: true)]
public class Checksums
{
    [Key("sha256")]
    public required string Sha256 { get; set; }
}

[MessagePackObject(keyAsPropertyName: true)]
public class ProjectOrDependency
{
    [Key("packageUri")]
    public string? PackageUri { get; set; }

    [Key("type")]
    public required string Type { get; set; }

    [Key("projectFileUri")]
    public string? ProjectFileUri { get; set; }

    [Key("checksums")]
    public Checksums? Checksums { get; set; }

    [Key("dependencies")]
    public Dictionary<string, ProjectOrDependency>? Dependencies { get; set; }
}