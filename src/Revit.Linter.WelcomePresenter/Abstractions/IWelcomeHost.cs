namespace Revit.Linter.WelcomePresenter.Abstractions;

/// <summary>
/// Supplies the facts and actions of the hosting add-in that the welcome wizard presents but does not own.
/// </summary>
public interface IWelcomeHost
{
    /// <summary>Gets the localized name of the ribbon tab that holds the add-in commands.</summary>
    string RibbonTabName { get; }

    /// <summary>Gets the directory where the add-in writes its log files.</summary>
    string LogDirectory { get; }

    /// <summary>Shows the add-in dockable panes that are currently hidden.</summary>
    /// <remarks>Requires a valid Revit API context.</remarks>
    void ShowPanes();

    /// <summary>Opens the quick-start guide in the default browser.</summary>
    void OpenQuickStart();

    /// <summary>Opens the user documentation in the default browser.</summary>
    void OpenDocumentation();

    /// <summary>Opens the support page in the default browser.</summary>
    void OpenSupport();
}
