namespace Revit.Linter.Core.Abstractions.Models;

public sealed class DiagnosticOverrideChangedEventArgs(
    DiagnosticOverrideState previous,
    DiagnosticOverrideState current) : EventArgs
{
    public DiagnosticOverrideState Previous { get; } = previous;
    public DiagnosticOverrideState Current { get; } = current;
}
