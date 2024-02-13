using Microsoft.CodeAnalysis;
using PklGenerator.Exceptions;

namespace PklGenerator;

[Generator]
public class PklSourceGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        // no-op
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var pklFiles = context.AdditionalFiles.Where(file => file.Path.EndsWith(".pkl"));
        
        foreach(var pklFile in pklFiles)
        {
            try
            {
                var name = Path.GetFileNameWithoutExtension(pklFile.Path);
                var generatedSource = GenerateSource(new FileSource(pklFile), name);
                context.AddSource($"Pkl{name}.g.cs", generatedSource);
            }
            catch (FileReadException e)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    PklDiagnosticDescriptors.FileReadErrorDiagnostic,
                    Location.None,
                    e.Path));
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

    private string GenerateSource(IParserSource source, string className)
    {
        //var reader = new 
        return $@"namespace PklGenerator
{{
    /* Source file:
{source.ReadAsText()} */

    public class {className}
    {{
        public string Host {{ get; set; }}
        public string Port {{ get; set; }}
    }}
}}";
    }
}