using Microsoft.Extensions.Logging;
using Revit.Linter.Diagnostic.Abstractions.Services;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Models;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Services;
using Revit.Linter.ElementIgnoring.Abstractions.Services;
using System.Diagnostics;
using Toolkit.Revit.Extensions;

namespace Revit.Linter.Diagnostic.Services;

internal sealed class DiagnosticService(
        IDiagnosticReportSender diagnosticReportSender,
        IDiagnosticCatalog diagnosticCatalog,
        IIgnoreElementDetector ignoreElementDetector,
        ILogger<DiagnosticService> logger)
    : IDiagnosticService
{
    private readonly ElementFilter _elementFilter = ElementFilterUtils.AllFilter();

    public DiagnosticServiceResult Execute(Document document, IEnumerable<ElementId> elementIds, View? view = null)
        => ExecuteSafely(() =>
        {
            Element[] elements = elementIds.Select(document.GetElement).ToArray();
            RunDocumentDiagnostics(document);
            RunElementDiagnostics(document, elements, view);
        });

    public DiagnosticServiceResult Execute(Document document, View? view = null)
        => ExecuteSafely(() =>
        {
            RunDocumentDiagnostics(document);
            RunElementDiagnostics(document, CollectElements(document, view), view);
        });

    private DiagnosticServiceResult ExecuteSafely(Action execute)
    {
        try
        {
            execute();

            return DiagnosticServiceResult.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Internal error");
            return DiagnosticServiceResult.Failed;
        }
    }

    private void RunDocumentDiagnostics(Document document)
    {
        foreach (DocumentDiagnosticRegistration registration in diagnosticCatalog.DocumentDiagnostics)
        {
            if (!registration.Override.IsActive || !registration.Filter.IsRelevantFor(document)) continue;

            (DiagnosticFeedback feedback, double duration) = Measure(
                () => registration.Diagnostic.Execute(document));
            if (feedback.Verdict == DiagnosticVerdict.Valid) continue;

            DocumentDiagnosticId identity = registration.Identity;
            diagnosticReportSender.Send(new DiagnosticReport(
                identity.Code,
                registration.Override.Severity,
                document,
                new DiagnosticReportMessage(identity.MessageFormat, CreateMessageArguments(
                    feedback, ("duration", duration), ("documentTitle", document.Title))),
                document,
                null,
                identity.IsObsolete,
                identity.ObsoleteDescription));
        }
    }

    private IEnumerable<Element> CollectElements(Document document, View? view) =>
        view is null
            ? new FilteredElementCollector(document).WherePasses(_elementFilter).ToElements()
            : new FilteredElementCollector(document, view.Id).WherePasses(_elementFilter).ToElements();

    private void RunElementDiagnostics(Document document, IEnumerable<Element> elements, View? view)
    {
        foreach (ElementDiagnosticRegistration registration in diagnosticCatalog.ElementDiagnostics)
        {
            if (!registration.Override.IsActive || !registration.DocumentFilter.IsRelevantFor(document)) continue;

            foreach (Element element in elements)
            {
                if (ignoreElementDetector.IsElementIgnored(registration.Identity.Code, element) ||
                    !registration.Filter.IsRelevantFor(document, element)) continue;

                (DiagnosticFeedback feedback, double duration) = Measure(
                    () => registration.Diagnostic.Execute(document, view, element));
                if (feedback.Verdict == DiagnosticVerdict.Valid) continue;

                ElementDiagnosticId identity = registration.Identity;
                diagnosticReportSender.Send(new DiagnosticReport(
                    identity.Code,
                    registration.Override.Severity,
                    document,
                    new DiagnosticReportMessage(identity.MessageFormat, CreateMessageArguments(
                        feedback,
                        ("duration", duration),
                        ("elementId", element.Id),
                        ("elementName", element.Name))),
                    element,
                    feedback.AdditionalTargetDependencies ?? [],
                    identity.IsObsolete,
                    identity.ObsoleteDescription));
            }
        }
    }

    private static (DiagnosticFeedback Feedback, double Duration) Measure(Func<DiagnosticFeedback> execute)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        DiagnosticFeedback feedback = execute();
        return (feedback, stopwatch.Elapsed.TotalMilliseconds);
    }

    private static (string, object)[] CreateMessageArguments(
        DiagnosticFeedback feedback,
        params (string Name, object Value)[] standardArguments)
    {
        if (feedback.AdditionalMessageArguments is not { Count: > 0 } additionalArguments)
            return standardArguments;

        var result = new (string, object)[standardArguments.Length + additionalArguments.Count];
        standardArguments.CopyTo(result, 0);
        int index = standardArguments.Length;
        foreach ((string name, object value) in additionalArguments)
            result[index++] = (name, value);
        return result;
    }

}
