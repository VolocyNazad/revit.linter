namespace Revit.Linter.Diagnostic.Services;

/// <summary>Controls what one diagnostic run measures, logs and batches.</summary>
/// <param name="LogDurations">Whether per-diagnostic durations are measured and logged.</param>
/// <param name="BatchReports">Whether a diagnostic's findings are sent to the report as one batch.</param>
/// <param name="RecordRunDurations">Whether the duration of each diagnostic is kept for the checks pane.</param>
/// <remarks>
/// A full run logs durations, batches findings because it can produce tens of thousands of rows, and
/// records the durations the checks pane shows. An incremental run happens after ordinary edits, so it
/// keeps all three off: it visits only the changed elements and its duration is not comparable.
/// </remarks>
internal sealed record DiagnosticRunOptions(bool LogDurations, bool BatchReports, bool RecordRunDurations)
{
    /// <summary>Gets the options of a full document or view run.</summary>
    public static DiagnosticRunOptions Full { get; } =
        new(LogDurations: true, BatchReports: true, RecordRunDurations: true);

    /// <summary>Gets the options of an incremental run after an element change.</summary>
    public static DiagnosticRunOptions Incremental { get; } =
        new(LogDurations: false, BatchReports: false, RecordRunDurations: false);
}
