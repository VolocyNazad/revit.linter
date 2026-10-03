namespace Revit.Linter.WelcomePresenter.Abstractions;

/// <summary>
/// Shows the welcome wizard that introduces the add-in and offers the optional setup steps.
/// </summary>
public interface IWelcomeWizard
{
    /// <summary>
    /// Shows the wizard when the current user has not seen it yet, or has not been offered the
    /// configuration examples for the running Revit version.
    /// </summary>
    /// <returns><see langword="true"/> when the wizard was shown; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// Only the pending steps are shown. The call blocks until the modal window closes; closing it in any
    /// way records the shown steps as completed. Must be called on the Revit UI thread.
    /// </remarks>
    bool ShowIfNeeded();

    /// <summary>
    /// Shows every wizard step regardless of the stored state.
    /// </summary>
    /// <remarks>The call blocks until the modal window closes. Must be called on the Revit UI thread.</remarks>
    void Show();
}
