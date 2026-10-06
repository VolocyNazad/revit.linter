using Microsoft.Extensions.Logging;
using Revit.Context.Abstractions.Services;
using Revit.Linter.Infrastructure.Utils;
using Revit.Linter.RunDiagnosticPresenter.Abstractions;

namespace Revit.Linter.Infrastructure.Services;

internal sealed class DiagnosticReportPaneActivator(
    IRevitContext revitContext,
    ILogger<DiagnosticReportPaneActivator> logger) : IDiagnosticReportPaneActivator
{
    public void Activate()
    {
        try
        {
            DockablePane? pane = revitContext.UIApplication?
                .GetDockablePane(DiagnosticReportPaneUtils.PaneId);

            pane?.Show();
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to activate the diagnostic report pane");
        }
    }
}
