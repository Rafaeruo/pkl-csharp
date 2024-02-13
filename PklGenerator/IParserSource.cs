using Microsoft.CodeAnalysis.Text;

namespace PklGenerator;

public interface IParserSource
{
    SourceText ReadAsText();
}