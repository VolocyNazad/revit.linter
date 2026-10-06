using Revit.Linter.WelcomePresenter.Abstractions.Models;

namespace Revit.Linter.WelcomePresenter.Abstractions;

/// <summary>
/// Supplies the facts and actions of the hosting add-in that the welcome wizard presents but does not own.
/// </summary>
public interface IWelcomeHost
{
    /// <summary>Gets a value indicating whether Revit currently has an active project or family document.</summary>
    bool HasOpenDocument { get; }

    /// <summary>Gets the localized name of the ribbon tab that holds the add-in commands.</summary>
    string RibbonTabName { get; }

    /// <summary>Gets the directory where the add-in writes its log files.</summary>
    string LogDirectory { get; }

    /// <summary>Shows the add-in dockable panes that are currently hidden.</summary>
    /// <remarks>Requires a valid Revit API context.</remarks>
    void ShowPanes();

    /// <summary>Queues the specified add-in pane to be shown and brought to the foreground.</summary>
    /// <remarks>The host performs the Revit UI operation in a valid API context.</remarks>
    void ShowPane(WelcomePane pane);

    /// <summary>Shows the dockable pane that contains the practical tour.</summary>
    void ShowPracticalTourPane();

    /// <summary>Hides the dockable pane that contains the practical tour.</summary>
    void HidePracticalTourPane();

    /// <summary>Shows a localized notification after the practical tour is completed.</summary>
    void ShowPracticalTourCompletedNotification();

    /// <summary>Queues a prepared disposable sample copy for the dedicated Revit ribbon command.</summary>
    void QueueTutorialSample(string path);

    /// <summary>Opens the quick-start guide in the default browser.</summary>
    void OpenQuickStart();

    /// <summary>Opens the user documentation in the default browser.</summary>
    void OpenDocumentation();

    /// <summary>Opens the support page in the default browser.</summary>
    void OpenSupport();
}
