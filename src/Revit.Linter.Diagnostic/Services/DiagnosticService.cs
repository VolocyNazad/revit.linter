using Microsoft.Extensions.Logging;
using Revit.Linter.Diagnostic.Abstractions.Services;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Models;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Services;
using Revit.Linter.ElementIgnoring.Abstractions.Services;
using System.Diagnostics;
using Revit.Sugar;

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
            using IDiagnosticCatalogSnapshotLease lease = diagnosticCatalog.AcquireSnapshot();
            DiagnosticCatalogSnapshot snapshot = lease.Snapshot;
            Element[] elements = elementIds.Select(document.GetElement).ToArray();
            bool documentDiagnosticsCompleted = RunDocumentDiagnostics(snapshot, document);
            bool elementDiagnosticsCompleted = RunElementDiagnostics(snapshot, document, elements, view);
            return documentDiagnosticsCompleted && elementDiagnosticsCompleted;
        });

    public DiagnosticServiceResult Execute(Document document, View? view = null)
        => ExecuteSafely(() =>
        {
            using IDiagnosticCatalogSnapshotLease lease = diagnosticCatalog.AcquireSnapshot();
            DiagnosticCatalogSnapshot snapshot = lease.Snapshot;
            bool documentDiagnosticsCompleted = RunDocumentDiagnostics(snapshot, document);
            bool elementDiagnosticsCompleted = RunElementDiagnostics(
                snapshot, document, CollectElements(document, view), view);
            return documentDiagnosticsCompleted && elementDiagnosticsCompleted;
        });

    private DiagnosticServiceResult ExecuteSafely(Func<bool> execute)
    {
        try
        {
            return execute() ? DiagnosticServiceResult.Success : DiagnosticServiceResult.Failed;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Internal error");
            return DiagnosticServiceResult.Failed;
        }
    }

    private bool RunDocumentDiagnostics(DiagnosticCatalogSnapshot snapshot, Document document)
    {
        bool completed = true;
        foreach (DocumentDiagnosticRegistration registration in snapshot.DocumentDiagnostics)
        {
            try
            {
                RunDocumentDiagnostic(registration, document);
            }
            catch (Exception exception)
            {
                ReportFailure(document, registration.Identity.Code, exception);
                completed = false;
            }
        }

        return completed;
    }

    private void RunDocumentDiagnostic(DocumentDiagnosticRegistration registration, Document document)
    {
        if (!registration.Override.IsActive || !registration.Filter.IsRelevantFor(document)) return;

        (DiagnosticFeedback[] feedbacks, double duration) = Measure(
            () => registration.Diagnostic.Execute(document).ToArray());

        foreach (DiagnosticFeedback feedback in feedbacks)
        {
            if (feedback.Verdict == DiagnosticVerdict.Valid) continue;

            DocumentDiagnosticId identity = registration.Identity;
            diagnosticReportSender.Send(new DiagnosticReport(
                identity.Code,
                registration.Override.Severity,
                document,
                new DiagnosticReportMessage(identity.MessageFormat, CreateMessageArguments(
                    feedback, ("duration", duration), ("documentTitle", document.Title))),
                document,
                feedback.AdditionalTargetDependencies,
                identity.IsObsolete,
                identity.ObsoleteDescription));
        }
    }

    private IEnumerable<Element> CollectElements(Document document, View? view) =>
        view is null
            ? new FilteredElementCollector(document).WherePasses(_elementFilter).ToElements()
            : new FilteredElementCollector(document, view.Id).WherePasses(_elementFilter).ToElements();

    private bool RunElementDiagnostics(
        DiagnosticCatalogSnapshot snapshot, Document document, IEnumerable<Element> elements, View? view)
    {
        bool completed = true;
        foreach (ElementDiagnosticRegistration registration in snapshot.ElementDiagnostics)
        {
            try
            {
                RunElementDiagnostic(registration, document, elements, view);
            }
            catch (Exception exception)
            {
                ReportFailure(document, registration.Identity.Code, exception);
                completed = false;
            }
        }

        return completed;
    }

    // The first failure stops the remaining elements of the same diagnostic: a faulty rule would otherwise
    // repeat the same error for every element.
    private void RunElementDiagnostic(
        ElementDiagnosticRegistration registration, Document document, IEnumerable<Element> elements, View? view)
    {
        if (!registration.Override.IsActive || !registration.DocumentFilter.IsRelevantFor(document)) return;

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

    private void ReportFailure(Document document, string diagnosticCode, Exception exception)
    {
        logger.LogError(exception, "Diagnostic {DiagnosticCode} failed", diagnosticCode);
        diagnosticReportSender.Send(new DiagnosticReport(
            diagnosticCode,
            DiagnosticSeverity.Error,
            document,
            new DiagnosticReportMessage(
                DiagnosticLocalizations.GetString("diagnosticFailed_message"),
                ("error", exception.Message)),
            document));
    }

    private static (T Result, double Duration) Measure<T>(Func<T> execute)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        T result = execute();
        return (result, stopwatch.Elapsed.TotalMilliseconds);
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
        foreach (KeyValuePair<string, object> pair in additionalArguments)
            result[index++] = (pair.Key, pair.Value);
        return result;
    }

}
