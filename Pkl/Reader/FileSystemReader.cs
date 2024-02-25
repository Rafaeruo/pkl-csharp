namespace Pkl.Reader;

public class FileSystemReader : IResourceReader, IModuleReader
{
    public FileSystemReader(string scheme)
    {
        Scheme = scheme;
    }

    public string Scheme { get; }

    public bool IsGlobbable { get; } = true;

    public bool HasHierarchicalUris { get; } = true;

    public bool IsLocal { get; } = true;

    public PathElement[] ListElements(Uri url)
    {
        var path = url.LocalPath.Trim('/');

        if (path == "")
        {
            path = ".";
        }

        var elements = Directory.GetFileSystemEntries(path);
        var returnPathElements = elements
            .Select(el => new FileInfo(el))
            .Where(el => !IsSymLink(el)) // copy Pkl's built-in `file` ModuleKey and don't follow symlinks.
            .Select(el => new PathElement
            {
                Name = el.Name,
                IsDirectory = IsDirectory(el)
            });

        return returnPathElements.ToArray();
    }

    private static bool IsSymLink(FileInfo fileInfo)
    {
        return fileInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
    }

    private static bool IsDirectory(FileInfo fileInfo)
    {
        return fileInfo.Attributes.HasFlag(FileAttributes.Directory);
    }

    byte[] IResourceReader.Read(Uri url)
    {
        return File.ReadAllBytes(url.AbsolutePath.TrimStart('/'));
    }

    string IModuleReader.Read(Uri url)
    {
        return File.ReadAllText(url.AbsolutePath.TrimStart('/'));
    }
}