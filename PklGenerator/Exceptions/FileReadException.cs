namespace PklGenerator.Exceptions;

public class FileReadException: Exception
{
    public readonly string Path;

    public FileReadException(string path) : base($"Failed to read the file {path}")
    {
        Path = path;
    }
}