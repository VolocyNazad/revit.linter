namespace Revit.Linter.TypeCatalogDiagnostics.Abstractions.Services;

/// <summary>
/// Controls watching the type catalog files of the open family documents.
/// </summary>
public interface ITypeCatalogChangesMonitor
{
    /// <summary>
    /// Starts watching the catalog files of the open families.
    /// </summary>
    /// <returns><see langword="true"/> when watching was started; <see langword="false"/> when it was already running.</returns>
    bool Run();

    /// <summary>
    /// Stops watching the catalog files.
    /// </summary>
    /// <returns><see langword="true"/> when watching was stopped; <see langword="false"/> when it was not running.</returns>
    bool Stop();
}
