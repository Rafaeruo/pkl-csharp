using Microsoft.CodeAnalysis;

namespace PklCSharp.Generator;

public class PklDiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor GeneralErrorDiagnostic = new DiagnosticDescriptor(
        "PKLGEN001",
        "Failed to generate source from pkl file",
        "Failed to parse and generate source code from the pkl document {0} | {1}",
        "PklGenerator",
        DiagnosticSeverity.Error,
        true);
    
    public static readonly DiagnosticDescriptor FileReadErrorDiagnostic = new DiagnosticDescriptor(
        "PKLGEN002",
        "Failed to read pkl file",
        "Failed to read the pkl document {0}",
        "PklGenerator",
        DiagnosticSeverity.Error,
        true);
}
