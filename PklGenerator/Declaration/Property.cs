using PklGenerator.Utilities;

namespace PklGenerator.Declaration;

public record Property(
    string Name,
    string Type,
    Visibility Visibility,
    bool IsNullable,
    string? DefaultValue);