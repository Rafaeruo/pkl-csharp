using Microsoft.CodeAnalysis.Text;

namespace PklGenerator.Generation;

public sealed record TranslationWarning(string Message, TextSpan Span);
