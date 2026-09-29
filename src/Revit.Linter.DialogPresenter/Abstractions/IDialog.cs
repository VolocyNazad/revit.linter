namespace Revit.Linter.DialogPresenter.Abstractions;

/// <summary>
/// Displays informational dialog content.
/// </summary>
public interface IDialog
{
    /// <summary>
    /// Displays an informational dialog for the specified request.
    /// </summary>
    /// <param name="request">The dialog request.</param>
    /// <param name="cancellationToken">A token that cancels the display operation.</param>
    /// <returns>A task that completes when the dialog closes or the operation is cancelled.</returns>
    Task Show(DialogRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Displays an informational dialog containing the specified content.
    /// </summary>
    /// <param name="content">The content to display.</param>
    /// <param name="cancellationToken">A token that cancels the display operation.</param>
    /// <returns>A task that completes when the dialog closes or the operation is cancelled.</returns>
    Task Show(object content, CancellationToken cancellationToken = default);
}
