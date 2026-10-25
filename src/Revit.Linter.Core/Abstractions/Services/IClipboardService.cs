namespace Revit.Linter.Core.Abstractions.Services;

/// <summary>
/// Provides the text clipboard of the interactive Windows session.
/// </summary>
public interface IClipboardService
{
    /// <summary>Replaces the clipboard text with the specified value.</summary>
    /// <param name="text">The text to place on the clipboard.</param>
    /// <returns><see langword="true"/> when the text was copied; otherwise <see langword="false"/>.</returns>
    /// <remarks>
    /// The clipboard is a shared Windows resource: another process may hold it open and make the copy fail.
    /// The implementation logs such a failure and returns <see langword="false"/> instead of throwing, so a
    /// refused copy never interrupts the command that requested it.
    /// </remarks>
    bool TrySetText(string text);
}
