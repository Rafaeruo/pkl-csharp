using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace PklGenerator;

[Generator]
public class PklSourceGenerator : ISourceGenerator
{
    private const string AttributeSource = @"namespace PklGenerator;

[AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple=true)]
public class PklSourceAttribute : Attribute
{
    public string Source { get; }
    public string Name { get; }
        
    public PklSourceAttribute(string source, string name)
    {
        Source = source;
        Name = name;
    }
}";
        
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForPostInitialization((pi) => pi.AddSource("PklSourceAttribute.g.cs", AttributeSource));
        context.RegisterForSyntaxNotifications(() => new PklSyntaxReciever());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (!context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.projectdir",
                out var projectPath))
        {
            throw new InvalidOperationException("Unable to locate project build directory");
        }
        
        var rx = (PklSyntaxReciever)context.SyntaxContextReceiver!;
        foreach ((var source, var name) in rx.Sources)
        {
            var path = Path.Combine(projectPath, source);
            var generatedSource = GenerateSource(new FileSource(path), name);
            context.AddSource($"Pkl{name}.g.cs", generatedSource);
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