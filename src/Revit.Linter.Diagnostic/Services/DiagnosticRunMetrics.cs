using Microsoft.Extensions.Logging;
using Revit.Linter.DocumentQueries.Abstractions.Models;
using Revit.Linter.DocumentQueries.Abstractions.Services;
using System.Diagnostics;

namespace Revit.Linter.Diagnostic.Services;

/// <summary>Owns the timings and counters of one diagnostic run and writes its log lines.</summary>
/// <remarks>
/// The service decides what to run and how to report findings; this type only measures and logs.
/// A full and an incremental run share it, so both describe themselves the same way.
/// </remarks>
internal sealed class DiagnosticRunMetrics
{
    private const long SlowDiagnosticMilliseconds = 100;

    private readonly Stopwatch _run = Stopwatch.StartNew();
    private readonly int _generation0Collections = GC.CollectionCount(0);
    private readonly int _generation2Collections = GC.CollectionCount(2);

    /// <summary>Gets the time elapsed since the run metrics were created.</summary>
    public long ElapsedMilliseconds => _run.ElapsedMilliseconds;

    /// <summary>Logs the start line with the run scope and the catalog size.</summary>
    /// <param name="logger">The diagnostic service logger.</param>
    /// <param name="document">The document being checked.</param>
    /// <param name="isViewScoped">Whether only the active view is checked.</param>
    /// <param name="snapshot">The catalog snapshot used by the run.</param>
    public void LogStarted(
        ILogger logger,
        Document document,
        bool isViewScoped,
        DiagnosticCatalogSnapshot snapshot)
        => logger.LogInformation(
            "Diagnostic run of {DocumentTitle} started (active view only: {IsViewScoped}) with {DocumentDiagnosticCount} document and {ElementDiagnosticCount} element diagnostics, {ActiveElementDiagnosticCount} of them active; preparation took {PreparationMilliseconds} ms",
            document.Title,
            isViewScoped,
            snapshot.DocumentDiagnostics.Count,
            snapshot.ElementDiagnostics.Count,
            snapshot.ElementDiagnostics.Count(registration => registration.State.IsEnabled),
            ElapsedMilliseconds);

    /// <summary>Logs how long element collection took.</summary>
    /// <param name="logger">The diagnostic service logger.</param>
    /// <param name="document">The document being checked.</param>
    /// <param name="elementCount">The number of collected elements.</param>
    /// <param name="elapsedMilliseconds">The collection duration.</param>
    public static void LogElementCollection(
        ILogger logger,
        Document document,
        int elementCount,
        long elapsedMilliseconds)
        => logger.LogDebug(
            "Collected {ElementCount} elements of {DocumentTitle} in {ElapsedMilliseconds} ms",
            elementCount, document.Title, elapsedMilliseconds);

    /// <summary>Logs the duration of one document diagnostic.</summary>
    /// <param name="logger">The diagnostic service logger.</param>
    /// <param name="diagnosticCode">The diagnostic code.</param>
    /// <param name="elapsedMilliseconds">The diagnostic duration.</param>
    public static void LogDocumentDiagnostic(
        ILogger logger, string diagnosticCode, long elapsedMilliseconds)
        => logger.Log(
            GetDurationLogLevel(elapsedMilliseconds),
            "Document diagnostic {DiagnosticCode} finished in {ElapsedMilliseconds} ms",
            diagnosticCode, elapsedMilliseconds);

    /// <summary>Logs the duration and counters of one element diagnostic.</summary>
    /// <param name="logger">The diagnostic service logger.</param>
    /// <param name="diagnosticCode">The diagnostic code.</param>
    /// <param name="elapsedMilliseconds">The diagnostic duration.</param>
    /// <param name="statistics">The counters collected for the diagnostic.</param>
    public static void LogElementDiagnostic(
        ILogger logger,
        string diagnosticCode,
        long elapsedMilliseconds,
        ElementDiagnosticRunStatistics statistics)
        => logger.Log(
            GetDurationLogLevel(elapsedMilliseconds),
            "Element diagnostic {DiagnosticCode} finished in {ElapsedMilliseconds} ms: {VisitedCount} elements visited, {IgnoredCount} ignored, {CheckedCount} checked, {FindingCount} findings; rule filter {FilterMilliseconds:F0} ms, ignore list {IgnoreMilliseconds:F0} ms, checking {CheckMilliseconds:F0} ms, publishing findings {PublishMilliseconds:F0} ms",
            diagnosticCode,
            elapsedMilliseconds,
            statistics.VisitedCount,
            statistics.IgnoredCount,
            statistics.CheckedCount,
            statistics.FindingCount,
            ToMilliseconds(statistics.FilterTicks),
            ToMilliseconds(statistics.IgnoreTicks),
            statistics.CheckMilliseconds,
            ToMilliseconds(statistics.PublishTicks));

    /// <summary>Logs the query cache statistics of the run.</summary>
    /// <param name="logger">The diagnostic service logger.</param>
    /// <param name="documentQueries">The query service whose statistics are drained.</param>
    /// <remarks>
    /// A cold cache shows as misses and a warm one as hits, so two runs of the same model can be compared:
    /// misses for a query the previous run already answered mean the cache was invalidated in between.
    /// </remarks>
    public static void LogQueryStatistics(ILogger logger, IDocumentQueryService documentQueries)
    {
        foreach (DocumentQueryStatistics statistics in documentQueries.TakeStatistics())
            logger.LogDebug(
                "Document query {Query}: {Hits} cache hits, {Misses} misses, {MissMilliseconds:F0} ms computing misses (nested queries included)",
                statistics.Query, statistics.Hits, statistics.Misses, statistics.MissMilliseconds);
    }

    /// <summary>Logs the completed run with its duration and managed-memory counters.</summary>
    /// <param name="logger">The diagnostic service logger.</param>
    /// <param name="document">The document that was checked.</param>
    /// <param name="isViewScoped">Whether only the active view was checked.</param>
    /// <param name="result">The run result.</param>
    public void LogCompleted(
        ILogger logger,
        Document document,
        bool isViewScoped,
        DiagnosticServiceResult result)
        => logger.LogInformation(
            "Diagnostic run of {DocumentTitle} (active view only: {IsViewScoped}) finished with {Result} in {ElapsedMilliseconds} ms; managed memory {ManagedMemoryMegabytes} MB, garbage collections during the run: {Generation0Collections} of generation 0, {Generation2Collections} of generation 2",
            document.Title,
            isViewScoped,
            result,
            ElapsedMilliseconds,
            GC.GetTotalMemory(forceFullCollection: false) / (1024 * 1024),
            GC.CollectionCount(0) - _generation0Collections,
            GC.CollectionCount(2) - _generation2Collections);

    /// <summary>Converts stopwatch ticks to milliseconds.</summary>
    /// <param name="stopwatchTicks">The elapsed stopwatch ticks.</param>
    /// <returns>The elapsed time in milliseconds.</returns>
    public static double ToMilliseconds(long stopwatchTicks)
        => stopwatchTicks * 1000.0 / Stopwatch.Frequency;

    /// <summary>Measures a diagnostic call without allocating a closure per element.</summary>
    /// <typeparam name="TState">The state passed to the measured call.</typeparam>
    /// <typeparam name="T">The call result.</typeparam>
    /// <param name="state">The state passed to the call.</param>
    /// <param name="execute">The call to measure.</param>
    /// <returns>The call result and its duration in milliseconds.</returns>
    public static (T Result, double Duration) Measure<TState, T>(TState state, Func<TState, T> execute)
    {
        long started = Stopwatch.GetTimestamp();
        T result = execute(state);
        return (result, ToMilliseconds(Stopwatch.GetTimestamp() - started));
    }

    // A full run writes one line per diagnostic. Only the slow ones are worth a place in the default log;
    // the rest is available when the log level is lowered to Debug.
    private static LogLevel GetDurationLogLevel(long elapsedMilliseconds)
        => elapsedMilliseconds >= SlowDiagnosticMilliseconds ? LogLevel.Information : LogLevel.Debug;
}

/// <summary>Counters collected for one element diagnostic during a full run.</summary>
internal sealed class ElementDiagnosticRunStatistics
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
