namespace Revit.Linter.DialogPresenter.Abstractions;

/// <summary>
/// A dialog with two buttons — confirm and close.
/// Unlike <see cref="IDialog"/> (an informational window with a single button), it returns the user's choice.
/// </summary>
public interface IConfirmationDialog
{
    /// <summary>
    /// Shows the dialog and returns true if the user pressed the confirm button
    /// (<see cref="ConfirmationDialogRequest.ConfirmButtonText"/>), and false if they closed the dialog
    /// some other way (close button, X) or the display was cancelled via <paramref name="cancellationToken"/>.
    /// </summary>
    Task<bool> Show(ConfirmationDialogRequest request, CancellationToken cancellationToken = default);
}
