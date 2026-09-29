namespace Revit.Linter.FixReportProvider.Abstractions.Models;

/// <summary>
/// Provides data for a fix-report notification.
/// </summary>
/// <param name="report">The fix report that was sent.</param>
public sealed class FixMessageSentEventArgs(FixReport report) : EventArgs
{
    /// <summary>
    /// Gets the fix report that was sent.
    /// </summary>
    public FixReport Report { get; } = report;
}
