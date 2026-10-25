namespace Revit.Linter.WelcomePresenter.Abstractions;

/// <summary>
/// Supplies the facts and actions of the hosting add-in that the welcome wizard presents but does not own.
/// </summary>
/// <remarks>
/// The host implements this contract together with <see cref="IPracticalTourHost"/>. A wizard step depends only
/// on the actions it uses, so the practical tour's pane, notification and diagnostics-filter actions do not
/// appear here.
/// </remarks>
public interface IWelcomeHost
{
    /// <summary>Gets a value indicating whether Revit currently has an active project or family document.</summary>
    bool HasOpenDocument { get; }

    /// <summary>Gets the localized name of the ribbon tab that holds the add-in commands.</summary>
    string RibbonTabName { get; }

    /// <summary>Shows the add-in dockable panes that are currently hidden.</summary>
    /// <remarks>Requires a valid Revit API context.</remarks>
    void ShowPanes();

    /// <summary>Queues a prepared disposable sample copy for the dedicated Revit ribbon command.</summary>
    void QueueTutorialSample(string path);

    /// <summary>Opens the quick-start guide in the default browser.</summary>
    void OpenQuickStart();

    /// <summary>Opens the user documentation in the default browser.</summary>
    void OpenDocumentation();

    /// <summary>Opens the support page in the default browser.</summary>
    void OpenSupport();
}
