using Microsoft.CodeAnalysis.Text;

namespace PklGenerator;

public interface IParserSource
{
    Stream ReadAsStream();
    SourceText ReadAsText();
}