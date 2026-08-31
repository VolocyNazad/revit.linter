namespace Revit.Linter.DialogPresenter.Abstractions;

/// <summary>
/// Диалог с двумя кнопками — подтверждение и закрытие.
/// В отличие от <see cref="IDialog"/> (информационное окно с одной кнопкой), возвращает выбор пользователя.
/// </summary>
public interface IConfirmationDialog
{
    /// <summary>
    /// Показывает диалог и возвращает true, если пользователь нажал кнопку подтверждения
    /// (<see cref="ConfirmationDialogRequest.ConfirmButtonText"/>), и false — если он закрыл диалог
    /// иначе (кнопка закрытия, крестик) или показ был отменён через <paramref name="cancellationToken"/>.
    /// </summary>
    Task<bool> Show(ConfirmationDialogRequest request, CancellationToken cancellationToken = default);
}
