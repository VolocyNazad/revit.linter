using Microsoft.Extensions.Logging;
using Revit.Linter.Diagnostic.Abstractions.Services;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Models;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Services;
using Revit.Linter.DocumentQueries.Abstractions.Models;
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
        => ExecuteSafely(() =>
        {
            using IDiagnosticCatalogSnapshotLease lease = diagnosticCatalog.AcquireSnapshot();
            DiagnosticCatalogSnapshot snapshot = lease.Snapshot;
            Element[] elements = elementIds.Select(document.GetElement).ToArray();
            bool documentDiagnosticsCompleted = RunDocumentDiagnostics(snapshot, document, logDurations: false);
            bool elementDiagnosticsCompleted = RunElementDiagnostics(
                snapshot, document, elements, view, logDurations: false);
            return documentDiagnosticsCompleted && elementDiagnosticsCompleted;
        });

    public DiagnosticServiceResult Execute(Document document, View? view = null)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        int generation0Collections = GC.CollectionCount(0);
        int generation2Collections = GC.CollectionCount(2);

        // Drops what incremental runs counted, so the statistics logged below describe this run only.
        documentQueries.TakeStatistics();

        DiagnosticServiceResult result = ExecuteSafely(() =>
        {
            using IDiagnosticCatalogSnapshotLease lease = diagnosticCatalog.AcquireSnapshot();
            DiagnosticCatalogSnapshot snapshot = lease.Snapshot;
            logger.LogInformation(
                "Diagnostic run of {DocumentTitle} started (active view only: {IsViewScoped}) with {DocumentDiagnosticCount} document and {ElementDiagnosticCount} element diagnostics, {ActiveElementDiagnosticCount} of them active; preparation took {PreparationMilliseconds} ms",
                document.Title,
                view is not null,
                snapshot.DocumentDiagnostics.Count,
                snapshot.ElementDiagnostics.Count,
                snapshot.ElementDiagnostics.Count(registration => registration.Override.IsActive),
                stopwatch.ElapsedMilliseconds);
            bool documentDiagnosticsCompleted = RunDocumentDiagnostics(snapshot, document, logDurations: true);
            Stopwatch collectionStopwatch = Stopwatch.StartNew();
            IReadOnlyList<Element> elements = CollectElements(document, view);
            logger.LogDebug(
                "Collected {ElementCount} elements of {DocumentTitle} in {ElapsedMilliseconds} ms",
                elements.Count, document.Title, collectionStopwatch.ElapsedMilliseconds);
            bool elementDiagnosticsCompleted = RunElementDiagnostics(
                snapshot, document, elements, view, logDurations: true);
            return documentDiagnosticsCompleted && elementDiagnosticsCompleted;
        });
        LogQueryStatistics();
        logger.LogInformation(
            "Diagnostic run of {DocumentTitle} (active view only: {IsViewScoped}) finished with {Result} in {ElapsedMilliseconds} ms; managed memory {ManagedMemoryMegabytes} MB, garbage collections during the run: {Generation0Collections} of generation 0, {Generation2Collections} of generation 2",
            document.Title,
            view is not null,
            result,
            stopwatch.ElapsedMilliseconds,
            GC.GetTotalMemory(forceFullCollection: false) / (1024 * 1024),
            GC.CollectionCount(0) - generation0Collections,
            GC.CollectionCount(2) - generation2Collections);
        return result;
    }

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

    // A cold cache shows as misses and a warm one as hits, so two runs of the same model can be compared:
    // misses for a query the previous run already answered mean the cache was invalidated in between.
    private void LogQueryStatistics()
    {
        foreach (DocumentQueryStatistics statistics in documentQueries.TakeStatistics())
            logger.LogDebug(
                "Document query {Query}: {Hits} cache hits, {Misses} misses, {MissMilliseconds:F0} ms computing misses (nested queries included)",
                statistics.Query, statistics.Hits, statistics.Misses, statistics.MissMilliseconds);
    }

    private bool RunDocumentDiagnostics(DiagnosticCatalogSnapshot snapshot, Document document, bool logDurations)
    {
        bool completed = true;
        foreach (DocumentDiagnosticRegistration registration in snapshot.DocumentDiagnostics)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                RunDocumentDiagnostic(registration, document);
            }
            catch (Exception exception)
            {
                ReportFailure(document, registration.Identity.Code, exception);
                completed = false;
            }

            if (logDurations && registration.Override.IsActive)
                logger.Log(
                    GetDurationLogLevel(stopwatch.ElapsedMilliseconds),
                    "Document diagnostic {DiagnosticCode} finished in {ElapsedMilliseconds} ms",
                    registration.Identity.Code, stopwatch.ElapsedMilliseconds);
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
        bool logDurations)
    {
        bool completed = true;

        // Resolved once for the run: asking the detector per element would resolve the document and
        // look its ignore information up again for every element of every diagnostic.
        IIgnoredElements ignoredElements = ignoreElementDetector.GetIgnoredElements(document);

        foreach (ElementDiagnosticRegistration registration in snapshot.ElementDiagnostics)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            ElementDiagnosticRunStatistics statistics = new();

            // A full run hands the findings of a diagnostic to the report as one batch instead of one
            // notification per finding. The batch is sent whether the diagnostic finished or failed, so
            // every finding produced before a failure still reaches the report, and it is sent before the
            // failure is reported, which keeps the order the reports had when they were sent one by one.
            // An incremental run produces a handful of findings and keeps sending them immediately.
            List<DiagnosticReport>? batch = logDurations ? new List<DiagnosticReport>() : null;
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
            if (logDurations && registration.Override.IsActive)
                logger.Log(
                    GetDurationLogLevel(stopwatch.ElapsedMilliseconds),
                    "Element diagnostic {DiagnosticCode} finished in {ElapsedMilliseconds} ms: {VisitedCount} elements visited, {IgnoredCount} ignored, {CheckedCount} checked, {FindingCount} findings; rule filter {FilterMilliseconds:F0} ms, ignore list {IgnoreMilliseconds:F0} ms, checking {CheckMilliseconds:F0} ms, publishing findings {PublishMilliseconds:F0} ms",
                    registration.Identity.Code,
                    stopwatch.ElapsedMilliseconds,
                    statistics.VisitedCount,
                    statistics.IgnoredCount,
                    statistics.CheckedCount,
                    statistics.FindingCount,
                    ToMilliseconds(statistics.FilterTicks),
                    ToMilliseconds(statistics.IgnoreTicks),
                    statistics.CheckMilliseconds,
                    ToMilliseconds(statistics.PublishTicks));
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
        if (!registration.Override.IsActive || !registration.DocumentFilter.IsRelevantFor(document)) return;

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
            (DiagnosticFeedback feedback, double duration) = Measure(
                () => registration.Diagnostic.Execute(document, view, element));
            statistics.CheckMilliseconds += duration;
            if (feedback.Verdict == DiagnosticVerdict.Valid) continue;

            statistics.FindingCount++;
            long publishStarted = Stopwatch.GetTimestamp();
            ElementDiagnosticId identity = registration.Identity;
            DiagnosticReport report = new(
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
                identity.ObsoleteDescription);
            if (batch is null)
                diagnosticReportSender.Send(report);
            else
                batch.Add(report);
            statistics.PublishTicks += Stopwatch.GetTimestamp() - publishStarted;
        }
    }

    // A full run writes one line per diagnostic. Only the slow ones are worth a place in the default log;
    // the rest is available when the log level is lowered to Debug.
    private const long SlowDiagnosticMilliseconds = 100;

    private static LogLevel GetDurationLogLevel(long elapsedMilliseconds) =>
        elapsedMilliseconds >= SlowDiagnosticMilliseconds ? LogLevel.Information : LogLevel.Debug;

    private static double ToMilliseconds(long stopwatchTicks) => stopwatchTicks * 1000.0 / Stopwatch.Frequency;

    private sealed class ElementDiagnosticRunStatistics
    {
        public int VisitedCount { get; set; }
        public int IgnoredCount { get; set; }
        public int CheckedCount { get; set; }
        public int FindingCount { get; set; }
        public long IgnoreTicks { get; set; }
        public long FilterTicks { get; set; }
        public double CheckMilliseconds { get; set; }
        public long PublishTicks { get; set; }
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
