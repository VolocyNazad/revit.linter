using Revit.Linter.OpenedDocuments.ViewModels;
using Revit.Linter.ThemeManaging.Abstractions.Services;
using Revit.Linter.DiagnosticReportPresenter.ViewModels;
using System.Windows;
using System.Windows.Controls;

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

    private void VisualizationButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: DiagnosticReportItemViewModel item } button)
            return;

        if (item.VisualizationPipelines.Count == 1)
        {
            item.ShowVisualizationCommand.Execute(null);
            return;
        }

        if (item.VisualizationPipelines.Count > 1 && button.ContextMenu is not null)
        {
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.IsOpen = true;
        }
    }
}
