namespace Revit.Linter.DialogPresenter.Abstractions;

/// <summary>
/// Describes the content displayed by an informational dialog.
/// </summary>
/// <param name="Content">The content to display.</param>
public sealed record DialogRequest(object Content);
