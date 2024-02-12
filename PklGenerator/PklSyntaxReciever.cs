using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PklGenerator;

internal class PklSyntaxReciever : ISyntaxContextReceiver
{
    public readonly List<(string source, string name)> Sources = new();
    
    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        if (context.Node is AttributeSyntax attrib
            && attrib.ArgumentList?.Arguments.Count == 2
            && context.SemanticModel.GetTypeInfo(attrib).Type?.ToDisplayString() == "PklGenerator.PklSourceAttribute")
        {
            var source = context.SemanticModel.GetConstantValue(attrib.ArgumentList.Arguments[0].Expression).ToString();
            var name = context.SemanticModel.GetConstantValue(attrib.ArgumentList.Arguments[1].Expression).ToString();
            Sources.Add((source, name));
        }
    }
}