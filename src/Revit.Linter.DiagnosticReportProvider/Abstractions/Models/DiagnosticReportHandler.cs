namespace Revit.Linter.DiagnosticReportProvider.Abstractions.Models;

/// <summary>
/// Handles a notification that a diagnostic report was sent.
/// </summary>
/// <param name="sender">The source of the notification.</param>
/// <param name="e">The event data containing the diagnostic report.</param>
public delegate void DiagnosticReportHandler(object? sender, DiagnosticMessageSentEventArgs e);

