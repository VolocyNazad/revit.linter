namespace Revit.Linter.ElementChangesProvider.Abstractions.Models;

/// <summary>
/// Handles a notification that a set of element changes was sent.
/// </summary>
/// <param name="sender">The source of the notification.</param>
/// <param name="e">The event data containing the reported changes.</param>
public delegate void ElementChangesHandler(object? sender, ElementChangesSentEventArgs e);

