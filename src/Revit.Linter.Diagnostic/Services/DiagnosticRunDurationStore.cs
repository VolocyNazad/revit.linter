using Microsoft.Extensions.Logging;
using Revit.Linter.Diagnostic.Abstractions.Models;
using Revit.Linter.Diagnostic.Abstractions.Services;

namespace Revit.Linter.Diagnostic.Services;

/// <summary>Keeps the durations of the last full run of each document in memory for the checks pane.</summary>
/// <remarks>
/// A full run records from the Revit thread, while the pane reads from the interface thread, so the
/// dictionary is guarded by a lock. The map is small: one entry per document and diagnostic code.
/// </remarks>
internal sealed class DiagnosticRunDurationStore(ILogger<DiagnosticRunDurationStore> logger)
    : IDiagnosticRunDurationStore
{
    private readonly object _sync = new();
    private readonly Dictionary<(string DocumentTitle, string DiagnosticCode), TimeSpan> _durations = [];

    public event EventHandler<DiagnosticRunDurationChangedEventArgs>? Changed;

    public void Record(string documentTitle, string diagnosticCode, TimeSpan duration)
    {
        lock (_sync)
            _durations[(documentTitle, diagnosticCode)] = duration;

        // An observer renders the value; a failing observer must not turn a finished diagnostic into a
        // failed run.
        var args = new DiagnosticRunDurationChangedEventArgs(documentTitle, diagnosticCode);
        Delegate[] handlers = Changed?.GetInvocationList() ?? [];
        foreach (Delegate handler in handlers)
        {
            try
            {
                ((EventHandler<DiagnosticRunDurationChangedEventArgs>)handler)(this, args);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception, "A run duration observer failed for diagnostic {DiagnosticCode}", diagnosticCode);
            }
        }
    }

    public bool TryGetDuration(string documentTitle, string diagnosticCode, out TimeSpan duration)
    {
        lock (_sync)
            return _durations.TryGetValue((documentTitle, diagnosticCode), out duration);
    }
}
