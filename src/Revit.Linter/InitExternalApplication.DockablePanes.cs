// Registration of the add-in dockable panes.

using Revit.Linter.DiagnosticListPresenter.Views;
using Revit.Linter.DiagnosticReportPresenter.Views;
using Revit.Linter.FixReportPresenter.Views;
using Revit.Linter.IgnoreListPresenter.Views;
using Revit.Linter.Infrastructure.Utils;
using Revit.Linter.PracticalTourPresenter.Views;
#if !AFTER2024
using Revit.Sugar;
#endif

namespace Revit.Linter;

internal sealed partial class InitExternalApplication
{
    private void RegisterDockablePanes()
    {
        RegisterDockablePane<DiagnosticReportView>(
            DiagnosticReportPaneUtils.PaneId, "diagnosticReport_dockablePane_title");
        RegisterDockablePane<FixReportView>(
            FixReportPaneUtils.PaneId, "fixReport_dockablePane_title", DiagnosticReportPaneUtils.PaneId);
        RegisterDockablePane<DiagnosticListView>(
            DiagnosticListPaneUtils.PaneId, "diagnosticList_dockablePane_title", DiagnosticReportPaneUtils.PaneId);
        RegisterDockablePane<IgnoreListView>(
            IgnoreListPaneUtils.PaneId, "ignoreList_dockablePane_title", DiagnosticReportPaneUtils.PaneId);
        Application.RegisterDockablePane(
            PracticalTourPaneUtils.PaneId,
            Localizer["practicalTour_dockablePane_title"],
            new DockablePaneProvider(GetService<PracticalTourView>(), dockPosition: DockPosition.Right));
    }

    private void RegisterDockablePane<TView>(
        DockablePaneId paneId, string titleResourceKey, DockablePaneId? tabBehind = null)
        where TView : System.Windows.FrameworkElement
        => Application.RegisterDockablePane(
            paneId, Localizer[titleResourceKey], new DockablePaneProvider(GetService<TView>(), tabBehind));
}

