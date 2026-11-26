namespace Revit.Linter.Diagnostic.Abstractions.Models;

/// <summary>Describes a recorded duration of one diagnostic.</summary>
/// <param name="documentTitle">The title of the document that ran the diagnostic.</param>
/// <param name="diagnosticCode">The code of the diagnostic whose duration was recorded.</param>
public sealed class DiagnosticRunDurationChangedEventArgs(
    string documentTitle,
    string diagnosticCode) : EventArgs
{
    /// <summary>Gets the title of the document that ran the diagnostic.</summary>
    public string DocumentTitle { get; } = documentTitle;

    /// <summary>Gets the code of the diagnostic whose duration was recorded.</summary>
    public string DiagnosticCode { get; } = diagnosticCode;
}
