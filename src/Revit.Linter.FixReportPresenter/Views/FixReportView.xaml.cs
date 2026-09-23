using Revit.Linter.OpenedDocuments.ViewModels;
using Revit.Linter.ThemeManaging.Abstractions.Services;

namespace Revit.Linter.FixReportPresenter.Views;

public sealed partial class FixReportView
{
    public FixReportView(
        OpenedDocumentsViewModel openedDocumentsViewModel,
        IThemeService themeService)
    {
        OpenedDocumentsViewModel = openedDocumentsViewModel;
        InitializeComponent();
        themeService.Register(this);
    }

    public OpenedDocumentsViewModel OpenedDocumentsViewModel { get; }
}
