using Revit.Linter.OpenedDocuments.ViewModels;
using Revit.Linter.ThemeManaging.Abstractions.Services;

namespace Revit.Linter.DiagnosticReportPresenter.Views;

public sealed partial class DiagnosticReportView
{
    /// <summary>
    /// Initializes a new diagnostic report view.
    /// </summary>
    /// <param name="openedDocumentsViewModel">The opened-document selector displayed by the view.</param>
    /// <param name="themeService">The service that applies and updates the application theme.</param>
    public DiagnosticReportView(
        OpenedDocumentsViewModel openedDocumentsViewModel,
        IThemeService themeService)
    {
        OpenedDocumentsViewModel = openedDocumentsViewModel;
        InitializeComponent();
        themeService.Register(this);
    }

    /// <summary>
    /// Gets the opened-document selector displayed by the view.
    /// </summary>
    public OpenedDocumentsViewModel OpenedDocumentsViewModel { get; }
}
