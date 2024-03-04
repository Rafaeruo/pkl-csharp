namespace Pkl.Projects;

/// <summary>
/// C# representation of pkl.Project#EvaluatorSettings.
/// </summary>
public class ProjectPackage
{
    public string Name { get; set; } = default!;
    public string BaseUri { get; set; } = default!;
    public string Version { get; set; } = default!;
    public string PackageZipUrl { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string[] Authors { get; set; } = default!;
    public string Website { get; set; } = default!;
    public string Documentation { get; set; } = default!;
    public string SourceCode { get; set; } = default!;
    public string SourceCodeUrlScheme { get; set; } = default!;
    public string License { get; set; } = default!;
    public string LicenseText { get; set; } = default!;
    public string IssueTracker { get; set; } = default!;
    public string[] ApiTests { get; set; } = default!;
    public string[] Exclude { get; set; } = default!;
    public string Uri { get; set; } = default!;
}
