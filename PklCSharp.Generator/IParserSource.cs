using Microsoft.CodeAnalysis.Text;

namespace PklCSharp.Generator;

public interface IParserSource
{
    SourceText ReadAsText();
}
