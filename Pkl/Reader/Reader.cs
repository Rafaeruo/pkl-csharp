namespace Pkl.Reader;

// Reader is the base implementation shared by a ResourceReader and a ModuleReader.
public interface IReader
{
    // scheme returns the scheme part of the URL that this reader can read.
    string Scheme { get; }

    // isGlobbable tells if this reader supports globbing via Pkl's `import*` and `glob*` keywords
    bool IsGlobbable { get; }

    // hasHierarchicalUris tells if the URIs handled by this reader are hierarchical.
    // Hierarchical URIs are URIs that have hierarchy elements like host, origin, query, and
    // fragment.
    //
    // A hierarchical URI must start with a "/" in its scheme specific part. For example, consider
    // the following two URIS:
    //
    //   flintstone:/persons/fred.pkl
    //   flintstone:persons/fred.pkl
    //
    // The first URI conveys name "fred.pkl" within parent "/persons/". The second URI
    // conveys the name "persons/fred.pkl" with no hierarchical meaning.
    bool HasHierarchicalUris { get; }

    // listElements returns the list of elements at a specified path.
    // If HasHierarchicalUris is false, path will be empty and ListElements should return all
    // available values.
    //
    // This method is only called if it is hierarchical and local, or if it is globbable.
    PathElement[] ListElements(Uri url);
}

// PathElement is an element within a base URI.
//
// For example, a PathElement with name "bar.txt" and is not a directory at base URI "file:///foo/"
// implies URI resource `file:///foo/bar.txt`.
public class PathElement
{
    // name is the name of the path element.
    public required string Name { get; set; }

    // isDirectory tells if the path element is a directory.
    public bool IsDirectory { get; set; }
}

// ResourceReader is a custom resource reader for Pkl.
//
// A ResourceReader registers the scheme that it is responsible for reading via Reader.Scheme. For
// example, a resource reader can declare that it reads a resource at secrets:MY_SECRET by returning
// "secrets" when Reader.Scheme is called.
//
// Resources are cached by Pkl for the lifetime of an Evaluator. Therefore, cacheing is not needed
// on the Go side as long as the same Evaluator is used.
//
// Resources are read via the following Pkl expressions:
//
//     read("myscheme:myresourcee")
//     read?("myscheme:myresource")
//   read*("myscheme:pattern*") // only if the resource is globabble
//
// To provide a custom reader, register it on EvaluatorOptions.ResourceReaders when building
// an Evaluator.
public interface IResourceReader : IReader
{
    // read reads the byte contents of this resource.
    byte[] Read(Uri url);
}

// ModuleReader is a custom module reader for Pkl.
//
// A ModuleReader registers the scheme that it is responsible for reading via Reader.Scheme. For
// example, a module reader can declare that it reads a resource at myscheme:myFile.pkl by returning
// "myscheme" when Reader.Scheme is called.
//
// Modules are cached by Pkl for the lifetime of an Evaluator. Therefore, cacheing is not needed
// on the Go side as long as the same Evaluator is used.
//
// Modules are read in Pkl via the import declaration:
//
//     import "myscheme:/myFile.pkl"
//   import* "myscheme:/*.pkl" // only when the reader is globbable
//
// Or via the import expression:
//
//     import("myscheme:myFile.pkl")
//   import*("myscheme:/myFile.pkl") // only when the reader is globbable
//
// To provide a custom reader, register it on EvaluatorOptions.ModuleReaders when building
// an Evaluator.
public interface IModuleReader : IReader
{
    // isLocal tells if the resources represented by this reader is considered local to the runtime.
    // A local module reader enables resolving triple-dot imports.
    bool IsLocal { get; }

    // read reads the string contents of this module.
    string Read(Uri url);
}