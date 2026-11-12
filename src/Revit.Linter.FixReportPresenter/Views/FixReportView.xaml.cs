using Revit.Linter.Presentation.ViewModels;
using Revit.Linter.ThemeManaging.Abstractions.Services;
using System.Windows.Controls.Primitives;

namespace Revit.Linter.FixReportPresenter.Views;

/// <summary>
/// Displays fix reports with document filtering and element-link interaction.
/// </summary>
public sealed partial class FixReportView
{
    /// <summary>
    /// Initializes the fix-report view and registers it for application theme updates.
    /// </summary>
    /// <param name="themeService">Applies the current application theme to the view.</param>
    public FixReportView(IThemeService themeService)
    {
        InitializeComponent();
        themeService.Register(this);
    }

    /// <summary>Initializes the view for the XAML designer, which has no application services.</summary>
    public FixReportView() => InitializeComponent();

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
