namespace Revit.Linter.FixReportProvider.Abstractions.Models;

/// <summary>
/// Handles a notification that a fix report was sent.
/// </summary>
/// <param name="sender">The source of the notification.</param>
/// <param name="e">The event data containing the fix report.</param>
public delegate void FixReportHandler(object? sender, FixMessageSentEventArgs e);

