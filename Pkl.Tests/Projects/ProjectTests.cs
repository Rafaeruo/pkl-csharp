using Pkl.Evaluation;
using Pkl.PklTypes;
using Pkl.Projects;

namespace Pkl.Tests.Projects;

public class ProjectTests
{
    private static readonly string project1Contents = @"
amends ""pkl:Project""

evaluatorSettings {
    timeout = 5.min
    rootDir = "".""
    noCache = false
    moduleCacheDir = ""cache/""
    env {
        [""one""] = ""1""
    }
    externalProperties {
        [""two""] = ""2""
    }
    modulePath {
        ""modulepath1/""
        ""modulepath2/""
    }
    allowedModules {
        ""foo:""
        ""bar:""
    }
    allowedResources {
        ""baz:""
        ""biz:""
    }
}

package {
    name = ""hawk""
    baseUri = ""package://example.com/hawk""
    version = ""0.5.0""
    description = ""Some project about hawks""
    packageZipUrl = ""https://example.com/hawk/0.5.0/hawk-0.5.0.zip""
    authors {
        ""Birdy Bird <birdy@bird.com>""
    }
    license = ""MIT""
    licenseText = ""# Some License text""
    sourceCode = ""https://example.com/my/repo""
    sourceCodeUrlScheme = ""https://example.com/my/repo/\(version)%{path}""
    documentation = ""https://example.com/my/docs""
    website = ""https://example.com/my/website""
    apiTests {
        ""apiTest1.pkl""
        ""apiTest2.pkl""
    }
    exclude { ""*.exe"" }
    issueTracker = ""https://example.com/my/issues""
}

dependencies {
    [""flamingos""] { uri = ""package://example.com/flamingos@0.5.0"" }
    [""storks""] = import(""../storks/PklProject"")
}

tests {
    ""test1.pkl""
    ""test2.pkl""
}";

    private static readonly string project2Contents = @"
amends ""pkl:Project""

package {
    name = ""storks""
    baseUri = ""package://example.com/storks""
    version = ""0.5.0""
    packageZipUrl = ""https://example.com/stork/\(version)/stork-\(version).zip""
}";

    [Fact]
    public async Task EvaluatorLoadsProjectWithDependency()
    {
        var tempDir = CreateTestProjects();
        var project1Path = Path.Combine(tempDir, "hawks", "PklProject");
        var project2Path = Path.Combine(tempDir, "storks", "PklProject");
        var manager = new EvaluatorManager.EvaluatorManager();
        var projectEvaluator = await manager.NewEvaluator(EvaluatorOptions.PreconfiguredOptons());

        Project project = null!;
        var loadProjectException = await Record.ExceptionAsync(async () =>
        {
            project = await projectEvaluator.LoadProject(project1Path); 
        });

        Assert.Null(loadProjectException);
        Assert.Equal($"file://{project1Path}", project.ProjectFileUri);

        // Evaluator settings
        Assert.Equivalent(new ProjectEvaluatorSettings
        {
            Timeout = new Duration { Value = 5, Unit = DurationUnit.Minute },
            NoCache = false,
            RootDir = ".",
            ModuleCacheDir = "cache/",
            Env = new Dictionary<string, string> { { "one", "1" } },
            ExternalProperties = new Dictionary<string, string> { { "two", "2" } },
            ModulePath = ["modulepath1/", "modulepath2/"],
            AllowedModules = ["foo:", "bar:"],
            AllowedResources = ["baz:", "biz:"]
        }, project.EvaluatorSettings);

        // Package
        Assert.Equivalent(new ProjectPackage
        {
            Name = "hawk",
            BaseUri = "package://example.com/hawk",
            Version = "0.5.0",
            Description = "Some project about hawks",
            PackageZipUrl = "https://example.com/hawk/0.5.0/hawk-0.5.0.zip",
            Authors = ["Birdy Bird <birdy@bird.com>"],
            License = "MIT",
            LicenseText = "# Some License text",
            SourceCode = "https://example.com/my/repo",
            SourceCodeUrlScheme = "https://example.com/my/repo/0.5.0%{path}",
            Documentation = "https://example.com/my/docs",
            Website = "https://example.com/my/website",
            ApiTests = ["apiTest1.pkl", "apiTest2.pkl"],
            Exclude = ["PklProject", "PklProject.deps.json", ".**", "*.exe"],
            IssueTracker = "https://example.com/my/issues",
            Uri = "package://example.com/hawk@0.5.0"
        }, project.Package);

        // Dependencies
        Assert.Equivalent(new ProjectDependencies
        {
            RemoteDependencies = new Dictionary<string, ProjectRemoteDependency>
              {
                  { "flamingos", new ProjectRemoteDependency { Uri = "package://example.com/flamingos@0.5.0" } }
              },
            LocalDependencies = new Dictionary<string, ProjectLocalDependency>
              {
                  { "storks", new ProjectLocalDependency
                      {
                          ProjectFileUri = $"file://{project2Path}",
                          PackageUri = "package://example.com/storks@0.5.0",
                          Dependencies = new ProjectDependencies
                          {
                              LocalDependencies = [],
                              RemoteDependencies = []
                          }
                      }
                  }
              }
        }, project.ProjectDependencies);

        // Tests
        Assert.Equivalent(new string[] { "test1.pkl", "test2.pkl" }, project.Tests);
    }

    private static string CreateTestProjects()
    {
        var tempDir = Path.GetTempPath();
        Directory.CreateDirectory(Path.Combine(tempDir, "hawks"));
        Directory.CreateDirectory(Path.Combine(tempDir, "storks"));

        File.WriteAllText(Path.Combine(tempDir, "hawks", "PklProject"), project1Contents);
        File.WriteAllText(Path.Combine(tempDir, "storks", "PklProject"), project2Contents);

        return tempDir;
    }
}
