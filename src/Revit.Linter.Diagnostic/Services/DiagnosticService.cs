using Microsoft.Extensions.Logging;
using Revit.Linter.Diagnostic.Abstractions.Services;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Models;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Services;
using Revit.Linter.DocumentQueries.Abstractions.Services;
using Revit.Linter.ElementIgnoring.Abstractions.Services;
using System.Diagnostics;

namespace Revit.Linter.Diagnostic.Services;

internal sealed class DiagnosticService(
        IDiagnosticReportSender diagnosticReportSender,
        IDiagnosticCatalog diagnosticCatalog,
        IIgnoreElementDetector ignoreElementDetector,
        IDocumentQueryService documentQueries,
        ILogger<DiagnosticService> logger)
    : IDiagnosticService
{
    public DiagnosticServiceResult Execute(Document document, IEnumerable<ElementId> elementIds, View? view = null)
        => ExecuteSafely(document, () =>
        {
            using IDiagnosticCatalogSnapshotLease lease = diagnosticCatalog.AcquireSnapshot();
            DiagnosticCatalogSnapshot snapshot = lease.Snapshot;
            // A selected element can disappear before the run starts, for example after an undo or a
            // synchronize; the deleted id must not turn into a null element inside every diagnostic.
            Element[] elements = elementIds
                .Select(elementId => document.GetElement(elementId))
                .OfType<Element>()
                .ToArray();
            bool documentDiagnosticsCompleted = RunDocumentDiagnostics(
                snapshot, document, DiagnosticRunOptions.Incremental);
            bool elementDiagnosticsCompleted = RunElementDiagnostics(
                snapshot, document, elements, view, DiagnosticRunOptions.Incremental);
            return documentDiagnosticsCompleted && elementDiagnosticsCompleted;
        });

    public DiagnosticServiceResult Execute(Document document, View? view = null)
    {
        var metrics = new DiagnosticRunMetrics();

        // Drops what incremental runs counted, so the statistics logged below describe this run only.
        documentQueries.TakeStatistics();

        DiagnosticServiceResult result = ExecuteSafely(document, () =>
        {
            using IDiagnosticCatalogSnapshotLease lease = diagnosticCatalog.AcquireSnapshot();
            DiagnosticCatalogSnapshot snapshot = lease.Snapshot;
            metrics.LogStarted(logger, document, view is not null, snapshot);
            bool documentDiagnosticsCompleted = RunDocumentDiagnostics(
                snapshot, document, DiagnosticRunOptions.Full);
            Stopwatch collectionStopwatch = Stopwatch.StartNew();
            IReadOnlyList<Element> elements = CollectElements(document, view);
            DiagnosticRunMetrics.LogElementCollection(
                logger, document, elements.Count, collectionStopwatch.ElapsedMilliseconds);
            bool elementDiagnosticsCompleted = RunElementDiagnostics(
                snapshot, document, elements, view, DiagnosticRunOptions.Full);
            return documentDiagnosticsCompleted && elementDiagnosticsCompleted;
        });

        DiagnosticRunMetrics.LogQueryStatistics(logger, documentQueries);
        metrics.LogCompleted(logger, document, view is not null, result);
        return result;
    }

    private DiagnosticServiceResult ExecuteSafely(Document document, Func<bool> execute)
    {
        try
        {
            return execute() ? DiagnosticServiceResult.Success : DiagnosticServiceResult.Failed;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected failure while running diagnostics for {DocumentTitle}", document.Title);
            return DiagnosticServiceResult.Failed;
        }
    }

    private bool RunDocumentDiagnostics(
        DiagnosticCatalogSnapshot snapshot,
        Document document,
        DiagnosticRunOptions options)
    {
        bool completed = true;
        // Resolved once for the run, like the element path below.
        IIgnoredElements ignoredElements = ignoreElementDetector.GetIgnoredElements(document);
        foreach (DocumentDiagnosticRegistration registration in snapshot.DocumentDiagnostics)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                RunDocumentDiagnostic(registration, document, ignoredElements);
            }
            catch (Exception exception)
            {
                ReportFailure(document, registration.Identity.Code, exception);
                completed = false;
            }

            if (options.LogDurations && registration.State.IsEnabled)
                DiagnosticRunMetrics.LogDocumentDiagnostic(
                    logger, registration.Identity.Code, stopwatch.ElapsedMilliseconds);
        }

        return completed;
    }

    private void RunDocumentDiagnostic(
        DocumentDiagnosticRegistration registration, Document document, IIgnoredElements ignoredElements)
    {
        if (!registration.State.IsEnabled || !registration.Filter.IsRelevantFor(document)) return;
        if (ignoredElements.IsDocumentIgnored(registration.Identity.Code)) return;

        (DiagnosticFeedback[] feedbacks, double duration) = DiagnosticRunMetrics.Measure(
            (registration, document),
            static state => state.registration.Diagnostic.Execute(state.document).ToArray());

        foreach (DiagnosticFeedback feedback in feedbacks)
        {
            if (feedback.Verdict == DiagnosticVerdict.Valid) continue;

            DocumentDiagnosticId identity = registration.Identity;
            diagnosticReportSender.Send(new DiagnosticReport(
                identity.Code,
                registration.State.Severity,
                document,
                new DiagnosticReportMessage(feedback.MessageFormat ?? identity.MessageFormat, CreateMessageArguments(
                    feedback, ("duration", duration), ("documentTitle", document.Title))),
                document,
                feedback.AdditionalTargetDependencies,
                identity.IsObsolete,
                identity.ObsoleteDescription));
        }
    }

    // Shares the element list with the diagnostics that scan the whole document during the same run.
    private IReadOnlyList<Element> CollectElements(Document document, View? view) =>
        documentQueries.GetElements(document, view);

    // Durations are logged only for full runs: an incremental run after an element change happens on
    // every edit and would flood the log with one line per diagnostic.
    private bool RunElementDiagnostics(
        DiagnosticCatalogSnapshot snapshot,
        Document document,
        IEnumerable<Element> elements,
        View? view,
        DiagnosticRunOptions options)
    {
        bool completed = true;

        // Resolved once for the run: asking the detector per element would resolve the document and
        // look its ignore information up again for every element of every diagnostic.
        IIgnoredElements ignoredElements = ignoreElementDetector.GetIgnoredElements(document);

        foreach (ElementDiagnosticRegistration registration in snapshot.ElementDiagnostics)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            var statistics = new ElementDiagnosticRunStatistics();

            // A full run hands the findings of a diagnostic to the report as one batch instead of one
            // notification per finding. The batch is sent whether the diagnostic finished or failed, so
            // every finding produced before a failure still reaches the report, and it is sent before the
            // failure is reported, which keeps the order the reports had when they were sent one by one.
            // An incremental run produces a handful of findings and keeps sending them immediately.
            List<DiagnosticReport>? batch = options.BatchReports ? [] : null;
            Exception? failure = null;
            try
            {
                RunElementDiagnostic(
                    registration, document, elements, view, ignoredElements, statistics, batch);
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            if (batch is { Count: > 0 })
            {
                long publishStarted = Stopwatch.GetTimestamp();
                try
                {
                    diagnosticReportSender.SendMany(batch);
                }
                catch (Exception exception)
                {
                    failure ??= exception;
                }

                statistics.PublishTicks += Stopwatch.GetTimestamp() - publishStarted;
            }

            if (failure is not null)
            {
                ReportFailure(document, registration.Identity.Code, failure);
                completed = false;
            }

            // Checking covers the diagnostic itself, and publishing covers handing each finding to the
            // report, which updates the report view. The ignore list and the rule filter are timed apart
            // because both run for every element of the document, whichever diagnostic is active.
            if (options.LogDurations && registration.State.IsEnabled)
                DiagnosticRunMetrics.LogElementDiagnostic(
                    logger,
                    registration.Identity.Code,
                    stopwatch.ElapsedMilliseconds,
                    statistics);
        }

        return completed;
    }

    // The first failure stops the remaining elements of the same diagnostic: a faulty rule would otherwise
    // repeat the same error for every element.
    private void RunElementDiagnostic(
        ElementDiagnosticRegistration registration,
        Document document,
        IEnumerable<Element> elements,
        View? view,
        IIgnoredElements ignoredElements,
        ElementDiagnosticRunStatistics statistics,
        List<DiagnosticReport>? batch)
    {
        if (!registration.State.IsEnabled || !registration.DocumentFilter.IsRelevantFor(document)) return;

        // A document-level ignore suppresses the whole diagnostic in this document, so its elements
        // are not even visited.
        if (ignoredElements.IsDocumentIgnored(registration.Identity.Code)) return;

        foreach (Element element in elements)
        {
            // The rule filter goes first: it rejects most elements almost for free, while the ignore
            // list has to read the element, so only the elements the rule applies to are asked about it.
            statistics.VisitedCount++;
            long filterStarted = Stopwatch.GetTimestamp();
            bool isRelevant = registration.Filter.IsRelevantFor(document, element);
            long ignoreStarted = Stopwatch.GetTimestamp();
            statistics.FilterTicks += ignoreStarted - filterStarted;
            if (!isRelevant) continue;

            bool isIgnored = ignoredElements.IsIgnored(registration.Identity.Code, element);
            statistics.IgnoreTicks += Stopwatch.GetTimestamp() - ignoreStarted;
            if (isIgnored)
            {
                statistics.IgnoredCount++;
                continue;
            }

            statistics.CheckedCount++;
            (DiagnosticFeedback feedback, double duration) = DiagnosticRunMetrics.Measure(
                (registration, document, view, element),
                static state => state.registration.Diagnostic.Execute(state.document, state.view, state.element));
            statistics.CheckMilliseconds += duration;
            if (feedback.Verdict == DiagnosticVerdict.Valid) continue;

            statistics.FindingCount++;
            long publishStarted = Stopwatch.GetTimestamp();
            ElementDiagnosticId identity = registration.Identity;
            DiagnosticReport report = new(
                identity.Code,
                registration.State.Severity,
                document,
                new DiagnosticReportMessage(feedback.MessageFormat ?? identity.MessageFormat, CreateMessageArguments(
                    feedback,
                    ("duration", duration),
                    ("elementId", element.Id),
                    ("elementName", element.Name))),
                element,
                feedback.AdditionalTargetDependencies ?? [],
                identity.IsObsolete,
                identity.ObsoleteDescription);
            if (batch is null)
                diagnosticReportSender.Send(report);
            else
                batch.Add(report);
            statistics.PublishTicks += Stopwatch.GetTimestamp() - publishStarted;
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
