using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace PklGenerator.Tests.Generation;

public record PklTestFile(string Path, string Content, bool Generate = true, string? Namespace = null, bool Enums = false);

public record GeneratorRun(
    IReadOnlyDictionary<string, string> Sources,
    IReadOnlyList<Diagnostic> GeneratorDiagnostics,
    IReadOnlyList<Diagnostic> CompilationDiagnostics);

/// <summary>Runs the real source generator through Roslyn, the way the compiler would.</summary>
public static class GeneratorHost
{
    public static GeneratorRun Run(string? rootNamespace, params PklTestFile[] files)
    {
        var additionalTexts = files.Select(f => (AdditionalText)new InMemoryText(f.Path, f.Content)).ToList();
        var options = new TestOptionsProvider(rootNamespace, files);

        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .Append(MetadataReference.CreateFromFile(typeof(Pkl.ModuleSource).Assembly.Location))
            .ToList();
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            Array.Empty<SyntaxTree>(),
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        var driver = CSharpGeneratorDriver.Create(
            new[] { (ISourceGenerator)new PklSourceGenerator() },
            additionalTexts,
            (CSharpParseOptions)CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest),
            options);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);

        var sources = driver.GetRunResult().Results.SelectMany(r => r.GeneratedSources)
            .ToDictionary(s => s.HintName, s => s.SourceText.ToString());
        return new GeneratorRun(sources, diagnostics, output.GetDiagnostics());
    }

    private sealed class InMemoryText : AdditionalText
    {
        private readonly string _content;

        public InMemoryText(string path, string content)
        {
            Path = path;
            _content = content;
        }

        public override string Path { get; }

        public override SourceText? GetText(CancellationToken cancellationToken = default) =>
            SourceText.From(_content);
    }

    private sealed class TestOptions : AnalyzerConfigOptions
    {
        private readonly Dictionary<string, string> _values;

        public TestOptions(Dictionary<string, string> values)
        {
            _values = values;
        }

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value) =>
            _values.TryGetValue(key, out value);
    }

    private sealed class TestOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly Dictionary<string, AnalyzerConfigOptions> _perFile = new();

        public TestOptionsProvider(string? rootNamespace, IEnumerable<PklTestFile> files)
        {
            var global = new Dictionary<string, string>();
            if (rootNamespace != null)
            {
                global["build_property.RootNamespace"] = rootNamespace;
            }

            GlobalOptions = new TestOptions(global);
            foreach (var file in files)
            {
                var values = new Dictionary<string, string>
                {
                    ["build_metadata.AdditionalFiles.PklGenerator_Generate"] = file.Generate ? "true" : "false",
                };
                if (file.Enums)
                {
                    values["build_metadata.AdditionalFiles.PklGenerator_Enums"] = "true";
                }

                if (file.Namespace != null)
                {
                    values["build_metadata.AdditionalFiles.PklGenerator_Namespace"] = file.Namespace;
                }

                _perFile[file.Path] = new TestOptions(values);
            }
        }

        public override AnalyzerConfigOptions GlobalOptions { get; }

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => _perFile[textFile.Path];
    }
}
