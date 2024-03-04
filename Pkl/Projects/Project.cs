using Pkl.Evaluation;

namespace Pkl.Projects;

/// <summary>
/// C# representation of pkl.Project.
/// </summary>
public class Project
{
    public string ProjectFileUri { get; set; } = default!;
    public ProjectPackage Package { get; set; } = default!;
    public ProjectEvaluatorSettings? EvaluatorSettings { get; set; }
    public string[] Tests { get; set; } = default!;

    /// <summary>
    /// Mapped to <see cref="ProjectDependencies"/>.
    /// </summary>
    public Dictionary<string, object> Dependencies { get; set; } = default!;
    private ProjectDependencies? _projectDependencies;
    public ProjectDependencies ProjectDependencies => _projectDependencies ??= BuildProjectDependencies(Dependencies);

    internal static ProjectDependencies BuildProjectDependencies(IDictionary<string, object> source)
    {
        var dependencies = new ProjectDependencies
        {
            LocalDependencies = new Dictionary<string, ProjectLocalDependency>(),
            RemoteDependencies = new Dictionary<string, ProjectRemoteDependency>()
        };

        foreach (var kvp in source)
        {
            var dependency = kvp.Value;

            if (dependency is Project project)
            {
                dependencies.LocalDependencies[kvp.Key] = new ProjectLocalDependency
                {
                    PackageUri = project.Package.Uri,
                    ProjectFileUri = project.ProjectFileUri,
                    Dependencies = BuildProjectDependencies(project.Dependencies)
                };
            }
            else if (dependency is ProjectRemoteDependency projectRemoteDependency)
            {
                dependencies.RemoteDependencies[kvp.Key] = projectRemoteDependency;
            }
        }

        return dependencies;
    }
}
