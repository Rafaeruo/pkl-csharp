using Microsoft.CodeAnalysis;
using PklGenerator.Declaration;
using PklGenerator.Exceptions;
using PklGenerator.Utilities;

namespace PklGenerator;

[Generator]
public class PklSourceGenerator : ISourceGenerator
{
    private const string GenerateAttributeKey = "build_metadata.AdditionalFiles.PklGenerator_Generate";

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
        
        // Temp stuff to test code gen
        var enumDec = new EnumDeclaration("GeneratedEnum", "PklGenerator");
        enumDec.AddMember("TestEnumMem");
        context.AddSource(enumDec.FileName, enumDec.ToSourceText());

        var classDec = new ClassDeclaration("GeneratedClass", "PklGenerator");
        classDec.AddProperty(new Property("AProp", "string", Visibility.Public, false, "\"Hello World\""));
        classDec.AddProperty(new Property("EnumVal", "GeneratedEnum", Visibility.Public, true, null));
        context.AddSource(classDec.FileName, classDec.ToSourceText());
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