namespace Revit.Linter.DialogPresenter.Abstractions;

/// <summary>
/// Describes the content and confirmation action displayed by a confirmation dialog.
/// </summary>
/// <param name="Content">The content to display.</param>
/// <param name="ConfirmButtonText">The label of the confirmation button.</param>
public sealed record ConfirmationDialogRequest(object Content, string ConfirmButtonText);
