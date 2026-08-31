namespace Revit.Linter.DialogPresenter.Abstractions;

/// <param name="Content"> Содержимое диалога </param>
/// <param name="ConfirmButtonText"> Текст кнопки подтверждения </param>
public sealed record ConfirmationDialogRequest(object Content, string ConfirmButtonText);
