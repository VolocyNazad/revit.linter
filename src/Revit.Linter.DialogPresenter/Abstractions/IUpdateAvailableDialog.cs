namespace Revit.Linter.DialogPresenter.Abstractions;

/// <summary>
/// Displays the three-button Revit update prompt and returns the user's decision.
/// </summary>
public interface IUpdateAvailableDialog
{
    /// <summary>Shows the update-available dialog and returns the chosen action.</summary>
    /// <param name="request">The available release to describe.</param>
    /// <param name="cancellationToken">A token that cancels the display operation.</param>
    /// <returns>
    /// The chosen action. A dialog closed with its title-bar button or cancelled through
    /// <paramref name="cancellationToken"/> reports <see cref="UpdateAvailableDialogChoice.Close"/>.
    /// </returns>
    Task<UpdateAvailableDialogChoice> Show(
        UpdateAvailableDialogRequest request,
        CancellationToken cancellationToken = default);
}
