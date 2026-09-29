namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>Describes a change in a diagnostic's effective override state.</summary>
/// <param name="previous">The state before the change.</param>
/// <param name="current">The state after the change.</param>
public sealed class DiagnosticOverrideChangedEventArgs(
    DiagnosticOverrideState previous,
    DiagnosticOverrideState current) : EventArgs
{
    /// <summary>Gets the state before the change.</summary>
    public DiagnosticOverrideState Previous { get; } = previous;
    /// <summary>Gets the state after the change.</summary>
    public DiagnosticOverrideState Current { get; } = current;
}
