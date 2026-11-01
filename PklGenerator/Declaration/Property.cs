using PklGenerator.Utilities;

namespace PklGenerator.Declaration;

/// <param name="PklName">
/// The Pkl property this holds, when that isn't the name with its first letter upper-cased. It is written as a
/// <c>[PklProperty]</c> attribute so the decoder can find the property.
/// </param>
public record Property(
    string Name,
    string Type,
    Visibility Visibility,
    bool IsNullable,
    string? DefaultValue,
    string? PklName = null);