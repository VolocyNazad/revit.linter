using Autodesk.Revit.DB;
using Microsoft.Extensions.Logging;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Models;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Services;
using Revit.Linter.WarningsHandling.Abstractions.Models;
using Revit.Linter.WarningsHandling.Abstractions.Services;
using Revit.Linter.WarningsHandling.Infrastructure.Extensions;

namespace Revit.Linter.WarningsHandling.Services;

internal sealed class RevitWarningsService(
    IDiagnosticReportSender diagnosticReportSender, 
    ILogger<RevitWarningsService> logger) : IRevitWarningsService
{
    public WarningsServiceResult Execute(Document document)  // todo Учитывать вид
    {
        try
        {
            HandleFailures(document);

            return WarningsServiceResult.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Internal error");

            return WarningsServiceResult.Failed;
        }
    }

    private void HandleFailures(Document document)
    {
        foreach (FailureMessage failureMessage in document.GetWarnings())
        {
            var failureSeverity = failureMessage.GetSeverity();

            DiagnosticSeverity severity = failureSeverity.ToDiagnosticSeverity();

            DiagnosticReportMessage diagnosticReportMessage = new(
                """
                    В документе с наименованием '{documentTitle}' обнаружены предупреждения. 
                    Элементы: {elementids}
                    Детали: {details}
                    """,
                ("documentTitle", document.Title),
                ("elementids", failureMessage.GetFailingElements()),
                ("details", failureMessage.GetDescriptionText()));

            DiagnosticReport diagnosticReport = new("RVT", severity, document, diagnosticReportMessage);

            diagnosticReportSender.Send(diagnosticReport);
        }
    }
}
