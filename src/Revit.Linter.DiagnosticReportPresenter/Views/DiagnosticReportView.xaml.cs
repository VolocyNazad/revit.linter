using Revit.Linter.OpenedDocuments.ViewModels;
using Revit.Linter.ThemeManaging.Abstractions.Services;

namespace Revit.Linter.DiagnosticReportPresenter.Views;

public sealed partial class DiagnosticReportView
{
    public DiagnosticReportView(
        OpenedDocumentsViewModel openedDocumentsViewModel,
        IThemeService themeService)
    {
        OpenedDocumentsViewModel = openedDocumentsViewModel;
        InitializeComponent();
        themeService.Register(this);
    }

    public OpenedDocumentsViewModel OpenedDocumentsViewModel { get; }
}
