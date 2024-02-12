using Microsoft.CodeAnalysis.Text;

namespace PklGenerator;

internal class FileSource : IParserSource
{
    private string FilePath { get; set; }

    public FileSource(string filePath)
    {
        FilePath = filePath;
    }
    
    public Stream ReadAsStream()
    {
        return File.OpenRead(FilePath);
    }

    public SourceText ReadAsText()
    {
        return SourceText.From(ReadAsStream());
    }
}