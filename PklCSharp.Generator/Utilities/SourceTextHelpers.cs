using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace PklCSharp.Generator.Utilities;

internal class StringBuilderText : SourceText
{
    private readonly StringBuilder _builder;
    public StringBuilderText(StringBuilder builder)
    {
        _builder = builder;
    }

    public override Encoding Encoding => Encoding.UTF8;
    public override int Length => _builder.Length;
    public override char this[int position] => _builder[position];

    public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
    {
        _builder.CopyTo(sourceIndex, destination, destinationIndex, count);
    }
}
