using Autodesk.Revit.DB;

namespace Revit.Linter.DiagnosticReportProvider.Abstractions.Models;

/// <summary>
/// Diagnostic report
/// </summary>
/// <param name="Code"> Diagnostic code </param>
/// <param name="Severity"> Severity </param>
/// <param name="Document"> The document the diagnostic was run in </param>
/// <param name="Target"> The object being checked </param>
/// <param name="TargetDependencies"> Dependency objects </param>
/// <param name="Message"> Diagnostic results message </param>
/// <param name="IsObsolete"> Indicates whether this is a report for an obsolete diagnostic </param>
/// <param name="ObsoleteDescription"> Description of the reason for obsolescence </param>
public sealed record DiagnosticReport(
    string Code, DiagnosticSeverity Severity, 
    Document Document, 
    DiagnosticReportMessage Message, 
    object? Target = null,
    object[]? TargetDependencies = null,
    bool IsObsolete = false, string ObsoleteDescription = "")
{
    public DateTime Created { get; } = DateTime.Now;
}
