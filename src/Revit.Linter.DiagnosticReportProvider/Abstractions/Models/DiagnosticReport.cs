using Autodesk.Revit.DB;

namespace Revit.Linter.DiagnosticReportProvider.Abstractions.Models;

/// <summary>
/// Describes the result of evaluating a diagnostic target.
/// </summary>
/// <param name="Code">The diagnostic code.</param>
/// <param name="Severity">The severity assigned to the finding.</param>
/// <param name="Document">The document in which the diagnostic was evaluated.</param>
/// <param name="Message">The message describing the diagnostic result.</param>
/// <param name="Target">The object evaluated by the diagnostic, if any.</param>
/// <param name="TargetDependencies">Objects on which the diagnostic target depends, if any.</param>
/// <param name="IsObsolete">A value indicating whether the report belongs to an obsolete diagnostic.</param>
/// <param name="ObsoleteDescription">The description of why the diagnostic is obsolete.</param>
public sealed record DiagnosticReport(
    string Code, DiagnosticSeverity Severity, 
    Document Document, 
    DiagnosticReportMessage Message, 
    object? Target = null,
    object[]? TargetDependencies = null,
    bool IsObsolete = false, string ObsoleteDescription = "")
{
    /// <summary>
    /// Gets the local time at which the report was created.
    /// </summary>
    public DateTime Created { get; } = DateTime.Now;
}
