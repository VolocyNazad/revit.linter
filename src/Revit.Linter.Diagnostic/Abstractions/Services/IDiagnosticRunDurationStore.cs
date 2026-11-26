using Revit.Linter.Diagnostic.Abstractions.Models;

namespace Revit.Linter.Diagnostic.Abstractions.Services;

/// <summary>
/// Remembers how long each diagnostic took during the last full run of each document, so the checks pane
/// can show the values of the active document.
/// </summary>
/// <remarks>
/// The values are run facts, not configuration: they live outside the catalog snapshot, which is rebuilt
/// and replaces every registration on each change. An incremental run after a model change visits only the
/// changed elements, so only a full run records a value. A document title identifies its values, and they
/// last until the add-in shuts down.
/// </remarks>
public interface IDiagnosticRunDurationStore
{
    /// <summary>Occurs after the duration of a diagnostic has been recorded.</summary>
    event EventHandler<DiagnosticRunDurationChangedEventArgs>? Changed;

    /// <summary>Records how long one diagnostic took during a full run of a document.</summary>
    /// <param name="documentTitle">The title of the document that ran the diagnostic.</param>
    /// <param name="diagnosticCode">The code of the diagnostic that ran.</param>
    /// <param name="duration">The time the diagnostic took, including publishing its findings.</param>
    void Record(string documentTitle, string diagnosticCode, TimeSpan duration);

    /// <summary>Tries to get the duration of the last full run of one diagnostic in a document.</summary>
    /// <param name="documentTitle">The title of the document whose run is asked about.</param>
    /// <param name="diagnosticCode">The code of the diagnostic.</param>
    /// <param name="duration">The recorded duration when the diagnostic has run at least once.</param>
    /// <returns><see langword="true"/> when the diagnostic has a recorded duration.</returns>
    bool TryGetDuration(string documentTitle, string diagnosticCode, out TimeSpan duration);
}
