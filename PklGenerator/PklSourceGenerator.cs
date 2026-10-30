using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using PklGenerator.Exceptions;
using PklGenerator.Generation;
using PklGenerator.Parsing;

namespace PklGenerator;

[Generator]
public class PklSourceGenerator : ISourceGenerator
{
    private const string GenerateAttributeKey = "build_metadata.AdditionalFiles.PklGenerator_Generate";
    private const string NamespaceAttributeKey = "build_metadata.AdditionalFiles.PklGenerator_Namespace";
    private const string RootNamespaceKey = "build_property.RootNamespace";
    private const string FallbackNamespace = "PklGenerated";

    public void Initialize(GeneratorInitializationContext context)
    {
        // no-op
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var pklFiles = context.AdditionalFiles.Where(file => file.Path.EndsWith(".pkl"));

        foreach(var pklFile in pklFiles)
        {
            var fileOptions = context.AnalyzerConfigOptions.GetOptions(pklFile);
            var hasAttribute = fileOptions.TryGetValue(GenerateAttributeKey, out var attributeValue);

            var shouldGenerate = hasAttribute
                && attributeValue is not null
                && attributeValue.Equals("true", StringComparison.OrdinalIgnoreCase);
            if (!shouldGenerate)
            {
                continue;
            }

            SourceText? sourceText = null;
            try
            {
                sourceText = new FileSource(pklFile).ReadAsText();
                var name = Path.GetFileNameWithoutExtension(pklFile.Path);
                var @namespace = ResolveNamespace(context, fileOptions);

                var module = Parser.ParseModule(sourceText.ToString());
                var result = ModuleTranslator.Translate(module, name, @namespace);

                foreach (var warning in result.Warnings)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        PklDiagnosticDescriptors.UnsupportedConstructDiagnostic,
                        ToLocation(pklFile.Path, sourceText, warning.Span),
                        warning.Message));
                }

                foreach (var type in result.Types)
                {
                    context.AddSource($"{name}.{type.FileName}", type.ToSourceText());
                }
            }
            catch (FileReadException e)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    PklDiagnosticDescriptors.FileReadErrorDiagnostic,
                    Location.None,
                    e.Path));
            }
            catch (PklParseException e) when (sourceText is not null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    PklDiagnosticDescriptors.ParseErrorDiagnostic,
                    ToLocation(pklFile.Path, sourceText, e.Span),
                    e.Message));
            }
            catch (Exception e)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    PklDiagnosticDescriptors.GeneralErrorDiagnostic,
                    Location.None,
                    pklFile.Path,
                    e.ToString()));
            }
        }
    }

    private static string ResolveNamespace(GeneratorExecutionContext context, AnalyzerConfigOptions fileOptions)
    {
        fileOptions.TryGetValue(NamespaceAttributeKey, out var fileNamespace);
        if (!string.IsNullOrWhiteSpace(fileNamespace))
        {
            return Naming.ToNamespace(fileNamespace, FallbackNamespace);
        }

        context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(RootNamespaceKey, out var rootNamespace);
        return Naming.ToNamespace(rootNamespace, FallbackNamespace);
    }

    private static Location ToLocation(string path, SourceText text, TextSpan span)
    {
        var start = Math.Min(span.Start, text.Length);
        var clamped = new TextSpan(start, Math.Min(span.Length, text.Length - start));
        return Location.Create(path, clamped, text.Lines.GetLinePositionSpan(clamped));
    }
}
