using System.Text;
using Microsoft.CodeAnalysis.Text;
using PklGenerator.Utilities;

namespace PklGenerator.Declaration;

internal abstract class TypeDeclaration
{
    protected TypeDeclaration(string fileNameWithoutExtension)
    {
        FileName = $"PklGen_{fileNameWithoutExtension}.g.cs";
    }
    
    public string FileName { get; }

    public SourceText ToSourceText()
    {
        var stringBuilder = new StringBuilder();
        var codeBuilder = new CodeBuilder(stringBuilder);
        
        BuildCode(codeBuilder);

        return new StringBuilderText(stringBuilder);
    }

    protected abstract void BuildCode(CodeBuilder builder);
}