using Revit.Linter.Presentation.ViewModels;
using Revit.Linter.ThemeManaging.Abstractions.Services;
using System.Windows.Controls.Primitives;

namespace Revit.Linter.DiagnosticReportPresenter.Views;

/// <summary>Displays the diagnostic findings of the active document inside a dockable pane.</summary>
public sealed partial class DiagnosticReportView
{
    /// <summary>
    /// Initializes a new diagnostic report view.
    /// </summary>
    /// <param name="themeService">The service that applies and updates the application theme.</param>
    public DiagnosticReportView(IThemeService themeService)
    {
        InitializeComponent();
        themeService.Register(this);
    }

    /// <summary>Initializes the view for the XAML designer, which has no application services.</summary>
    public DiagnosticReportView() => InitializeComponent();

    /// <summary>
    /// Marks the expanded list as closed in its row.
    /// </summary>
    /// <remarks>
    /// A popup that closes by itself (a click outside it) does not write that state back through the
    /// two-way binding, so the row would keep an open state and its next expander click would do nothing.
    /// The popup raises a plain CLR event here, which no behavior trigger can observe.
    /// </remarks>
    /// <param name="sender">The popup that closed.</param>
    /// <param name="e">The event data.</param>
    private void MessageListPopup_Closed(object? sender, EventArgs e)
    {
        if (sender is Popup { DataContext: ReportRowViewModel row })
            row.CollapseListCommand.Execute(null);
    }
}
