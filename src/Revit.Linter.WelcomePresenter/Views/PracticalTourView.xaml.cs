namespace Revit.Linter.WelcomePresenter.Views;

/// <summary>Displays the practical tour inside a Revit dockable pane.</summary>
public sealed partial class PracticalTourView
{
    private bool _wasVisible;

    /// <summary>Initializes the practical-tour pane content.</summary>
    public PracticalTourView()
    {
        InitializeComponent();
        IsVisibleChanged += View_IsVisibleChanged;
    }

    /// <summary>Occurs when the user hides a pane that was visible.</summary>
    public event EventHandler? PaneHidden;

    private void View_IsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs args)
    {
        if (IsVisible)
        {
            _wasVisible = true;
            return;
        }

        if (!_wasVisible) return;
        _wasVisible = false;
        PaneHidden?.Invoke(this, EventArgs.Empty);
    }
}
