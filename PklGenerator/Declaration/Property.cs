using PklGenerator.Utilities;

namespace PklGenerator.Declaration;

internal record Property(
    string Name,
    string Type,
    Visibility Visibility,
    bool IsNullable,
    string? DefaultValue);