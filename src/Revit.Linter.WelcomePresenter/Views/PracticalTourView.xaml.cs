namespace Revit.Linter.WelcomePresenter.Views;

/// <summary>Displays the practical tour inside a Revit dockable pane.</summary>
/// <remarks>
/// Pane visibility never drives the tour session: WPF reports invisible for tab switches and
/// Revit-driven hides as well as for the user closing the pane, so ending the session here
/// would leave a visible pane without content. The session ends only through its explicit
/// paths (stop, opt-out, restart, completion, failure, a new start); reopening the pane
/// resumes from the saved progress.
/// </remarks>
public sealed partial class PracticalTourView
{
    /// <summary>Initializes the practical-tour pane content.</summary>
    public PracticalTourView()
    {
        InitializeComponent();
    }
}
