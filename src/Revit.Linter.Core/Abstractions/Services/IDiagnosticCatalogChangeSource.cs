namespace Revit.Linter.Core.Abstractions.Services;

/// <summary>Reports external changes that require the diagnostic catalog to be refreshed.</summary>
public interface IDiagnosticCatalogChangeSource
{
    /// <summary>Subscribes a listener to future change notifications.</summary>
    /// <param name="listener">The callback invoked when the source changes.</param>
    /// <returns>A subscription that removes the listener when disposed.</returns>
    IDisposable OnChange(Action listener);
}
