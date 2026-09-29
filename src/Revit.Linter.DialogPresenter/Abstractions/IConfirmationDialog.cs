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
    /// <param name="request">The confirmation dialog request.</param>
    /// <param name="cancellationToken">A token that cancels the display operation.</param>
    /// <returns>A task containing <see langword="true"/> when the user confirms; otherwise, <see langword="false"/>.</returns>
    Task<bool> Show(ConfirmationDialogRequest request, CancellationToken cancellationToken = default);
}
