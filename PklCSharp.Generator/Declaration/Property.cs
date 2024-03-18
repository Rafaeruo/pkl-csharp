using PklCSharp.Generator.Utilities;

namespace PklCSharp.Generator.Declaration;

public record Property(
    string Name,
    string Type,
    Visibility Visibility,
    bool IsNullable,
    string? DefaultValue);
