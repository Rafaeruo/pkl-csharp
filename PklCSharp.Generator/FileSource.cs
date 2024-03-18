using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using PklCSharp.Generator.Exceptions;

namespace PklCSharp.Generator;

internal class FileSource : IParserSource
{
    private readonly AdditionalText _file;

    public FileSource(AdditionalText file)
    {
        _file = file;
    }

    public SourceText ReadAsText()
    {
        var text = _file.GetText();

        if (text == null)
        {
            throw new FileReadException(_file.Path);
        }
        
        return text;
    }
}
