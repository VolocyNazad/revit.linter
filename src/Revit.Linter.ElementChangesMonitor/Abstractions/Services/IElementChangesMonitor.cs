namespace Revit.Linter.ElementChangesMonitor.Abstractions.Services;

/// <summary>
/// Controls monitoring of element changes in Revit documents.
/// </summary>
public interface IElementChangesMonitor
{
    /// <summary>
    /// Starts monitoring document changes.
    /// </summary>
    /// <returns><see langword="true"/> when monitoring was started; <see langword="false"/> when it was already running.</returns>
    bool Run();

    /// <summary>
    /// Stops monitoring document changes.
    /// </summary>
    /// <returns><see langword="true"/> when monitoring was stopped; <see langword="false"/> when it was not running.</returns>
    bool Stop();
}

