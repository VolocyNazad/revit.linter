namespace Revit.Linter.DialogPresenter.Abstractions;

/// <param name="Content"> Dialog content </param>
/// <param name="ConfirmButtonText"> Confirm button text </param>
public sealed record ConfirmationDialogRequest(object Content, string ConfirmButtonText);
