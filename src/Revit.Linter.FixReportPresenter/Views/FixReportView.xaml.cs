using Revit.Linter.OpenedDocuments.ViewModels;
using Revit.Linter.ThemeManaging.Abstractions.Services;

namespace Revit.Linter.FixReportPresenter.Views;

/// <summary>
/// Displays fix reports with document filtering and element-link interaction.
/// </summary>
public sealed partial class FixReportView
{
    /// <summary>
    /// Initializes the fix-report view, its opened-document selector, and application theme registration.
    /// </summary>
    /// <param name="openedDocumentsViewModel">Provides the documents currently open in Revit.</param>
    /// <param name="themeService">Applies the current application theme to the view.</param>
    public FixReportView(
        OpenedDocumentsViewModel openedDocumentsViewModel,
        IThemeService themeService)
    {
        OpenedDocumentsViewModel = openedDocumentsViewModel;
        InitializeComponent();
        themeService.Register(this);
    }

    /// <summary>
    /// Gets the shared view model used by the document selector.
    /// </summary>
    public OpenedDocumentsViewModel OpenedDocumentsViewModel { get; }
}
