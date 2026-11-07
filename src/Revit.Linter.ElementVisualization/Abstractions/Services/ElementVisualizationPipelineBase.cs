using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.ElementAccentor.Abstractions.Models;
using Revit.Linter.ElementAccentor.Abstractions.Services;
using Revit.Linter.ElementVisualization.Abstractions.Models;

namespace Revit.Linter.ElementVisualization.Abstractions.Services;

/// <summary>Provides ordered execution and reverse-order restoration for visualization steps.</summary>
/// <param name="identity">The diagnostic identity associated with the pipeline.</param>
/// <param name="value">The user-facing pipeline name.</param>
/// <param name="services">The services that execute element accent operations.</param>
/// <param name="overrideElementGraphicsService">Applies per-element graphics.</param>
/// <param name="overrideFilterGraphicsService">Applies graphics through temporary filters.</param>
/// <param name="setCategoryVisibilityService">Leaves only the given categories visible.</param>
/// <param name="viewSettingsService">Temporarily changes whole-view settings.</param>
/// <param name="logger">Writes visualization lifecycle diagnostics.</param>
public abstract class ElementVisualizationPipelineBase(
    ElementDiagnosticId identity,
    string value,
    IEnumerable<IAccentElementsService> services,
    IOverrideElementGraphicsService overrideElementGraphicsService,
    IOverrideFilterGraphicsService overrideFilterGraphicsService,
    ISetCategoryVisibilityService setCategoryVisibilityService,
    IViewSettingsService viewSettingsService,
    ILogger logger) : IElementVisualizationPipeline
{
    private readonly IReadOnlyDictionary<AccentElementsType, IAccentElementsService> _services =
        services.ToDictionary(service => service.Type);
    private readonly List<IRestoreSession> _sessions = [];
    private int _consecutiveRestoreFailures;
    private Document? _sessionDocument;
    private bool _disposed;

    /// <inheritdoc />
    public ElementDiagnosticId Identity { get; } = identity;

    /// <inheritdoc />
    public string Value { get; } = value;

    /// <summary>Gets the steps to execute in declaration order.</summary>
    protected abstract IReadOnlyList<ElementVisualizationStep> Steps { get; }

    /// <inheritdoc />
    public bool Apply(ElementVisualizationContext context)
    {
        if (_disposed)
            throw new ObjectDisposedException(GetType().FullName);

        logger.LogInformation(
            "Applying visualization pipeline {VisualizationName} for diagnostic {DiagnosticCode} " +
            "on document {DocumentTitle}, view {ViewName}. Steps: {StepCount}; element sets: {ElementSetCount}",
            Value, Identity.Code, context.Document.Title, context.View.Name, Steps.Count, context.ElementSets.Count);
        Restore();

        List<IRestoreSession> appliedSessions = [];
        using TransactionGroup? transactionGroup = StartTransactionGroup(
            context.Document, ElementVisualizationLocalizations.GetString("applyVisualization_transactionGroup", Value));
        Stopwatch stopwatch = Stopwatch.StartNew();
        Transaction? stepTransaction = null;
        bool viewActivated = false;
        try
        {
            for (int index = 0; index < Steps.Count; index++)
            {
                ElementVisualizationStep step = Steps[index];

                // Revit rejects a change of the active view while a transaction is open, and a step
                // that needs the view on screen cannot run before the view is active. The declared
                // step order is kept: the preceding steps are committed, then the view is activated,
                // and only then the remaining steps run.
                if (step.RequiresActiveView && !viewActivated)
                {
                    CommitStepTransaction(ref stepTransaction);
                    viewActivated = TryActivateView(context);
                }

                if (step.RequiresTransaction)
                    EnsureStepTransaction(ref stepTransaction, context.Document);
                else
                    CommitStepTransaction(ref stepTransaction);

                ElementId[] elementIds = ResolveElementIds(context, step.ElementSetKeys);
                LogStepApplying(index, step, elementIds);
                Stopwatch stepStopwatch = Stopwatch.StartNew();
                appliedSessions.Add(ExecuteStep(context, step, elementIds));
                LogStepApplied(index, step, stepStopwatch.ElapsedMilliseconds);
            }

            CommitStepTransaction(ref stepTransaction);
            if (!viewActivated)
                TryActivateView(context);
            Assimilate(transactionGroup);
            _sessions.AddRange(appliedSessions);
            _sessionDocument = context.Document;
            _consecutiveRestoreFailures = 0;
            logger.LogInformation(
                "Visualization pipeline {VisualizationName} for diagnostic {DiagnosticCode} " +
                "applied successfully in {ElapsedMilliseconds:F0} ms",
                Value, Identity.Code, stopwatch.Elapsed.TotalMilliseconds);
            return true;
        }
        catch (Exception exception)
        {
            RollBack(stepTransaction);
            RollBack(transactionGroup);
            Exception? cleanupError = RestoreSessions(appliedSessions);
            logger.LogError(
                exception,
                "Visualization pipeline {VisualizationName} for diagnostic {DiagnosticCode} failed; " +
                "restoring {SessionCount} completed sessions",
                Value, Identity.Code, appliedSessions.Count);
            throw new InvalidOperationException(
                $"Visualization pipeline '{Value}' failed for diagnostic '{Identity.Code}'.",
                cleanupError is null ? exception : new AggregateException(exception, cleanupError));
        }
    }

    // A step that changes the document needs one transaction. Consecutive such steps share it, so a
    // pipeline usually runs in a single transaction; a step that needs the active view splits the
    // pipeline into a segment before the activation and one after it.
    private void EnsureStepTransaction(ref Transaction? transaction, Document document)
    {
        if (transaction is not null || document.IsModifiable) return;

        Transaction started = new(
            document, ElementVisualizationLocalizations.GetString("applyVisualization_transactionStep", Value));
        try
        {
            if (started.Start() != TransactionStatus.Started)
                throw new InvalidOperationException("Failed to start a visualization step transaction.");
        }
        catch
        {
            started.Dispose();
            throw;
        }

        transaction = started;
    }

    private static void CommitStepTransaction(ref Transaction? transaction)
    {
        Transaction? current = transaction;
        if (current is null) return;
        transaction = null;
        try
        {
            if (current.GetStatus() == TransactionStatus.Started
                && current.Commit() != TransactionStatus.Committed)
                throw new InvalidOperationException("Failed to commit a visualization step transaction.");
        }
        finally
        {
            current.Dispose();
        }
    }

    // Activation runs between two committed transactions, which is the only state in which Revit
    // accepts a change of the active view.
    private bool TryActivateView(ElementVisualizationContext context)
    {
        Action<View>? activateView = context.ViewActivator;
        if (activateView is null) return false;
        if (context.Document.IsModifiable)
        {
            logger.LogDebug(
                "The visualization view {ViewName} was not activated for pipeline {VisualizationName} " +
                "because an outer transaction keeps the document modifiable",
                context.View.Name, Value);
            return true;
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        activateView(context.View);
        logger.LogInformation(
            "Activated visualization view {ViewName} for pipeline {VisualizationName} in {ElapsedMilliseconds:F0} ms",
            context.View.Name, Value, stopwatch.Elapsed.TotalMilliseconds);
        return true;
    }

    private IRestoreSession ExecuteStep(
        ElementVisualizationContext context, ElementVisualizationStep step, ElementId[] elementIds)
        => step switch
        {
            AccentElementsStep accent => ResolveService(accent.Type)
                .Apply(context.Document, context.View, elementIds),
            OverrideElementsStep graphics => overrideElementGraphicsService
                .Apply(context.Document, context.View, elementIds, graphics.Style),
            OverrideFilterStep filter => overrideFilterGraphicsService
                .Apply(context.Document, context.View, elementIds, filter.Style),
            SetCategoryVisibilityStep setCategoryVisibility => setCategoryVisibilityService
                .Apply(context.Document, context.View, setCategoryVisibility.Categories),
            DetailLevelStep detailLevel => viewSettingsService
                .ApplyDetailLevel(context.Document, context.View, detailLevel.Level),
            DisplayStyleStep displayStyle => viewSettingsService
                .ApplyDisplayStyle(context.Document, context.View, displayStyle.Style),
            _ => throw new InvalidOperationException(
                $"Unsupported visualization step '{step.GetType().Name}'.")
        };

    private void LogStepApplying(int index, ElementVisualizationStep step, ElementId[] elementIds)
        => logger.LogInformation(
            "Applying visualization step {StepNumber}/{StepCount}: {StepType}. " +
            "Element sets: {ElementSetKeys}; elements: {ElementCount}",
            index + 1, Steps.Count, step.GetType().Name,
            step.ElementSetKeys.Count == 0 ? "<all>" : string.Join(", ", step.ElementSetKeys),
            elementIds.Length);

    private void LogStepApplied(int index, ElementVisualizationStep step, long elapsedMilliseconds)
        => logger.LogInformation(
            "Visualization step {StepNumber}/{StepCount} completed: {StepType} in {ElapsedMilliseconds:F0} ms",
            index + 1, Steps.Count, step.GetType().Name, elapsedMilliseconds);

    /// <inheritdoc />
    public void Restore()
    {
        int sessionCount = _sessions.Count;
        if (sessionCount > 0)
            logger.LogInformation(
                "Restoring visualization pipeline {VisualizationName} for diagnostic {DiagnosticCode}. " +
                "Sessions: {SessionCount}",
                Value, Identity.Code, sessionCount);

        // A closed document invalidates the stored Document wrapper, and starting a transaction
        // group from it would throw before the sessions are classified. The sessions restore
        // nothing without their document, so they are restored individually instead.
        Document? sessionDocument = _sessionDocument;
        using TransactionGroup? transactionGroup = sessionDocument is { IsValidObject: true }
            ? StartTransactionGroup(sessionDocument, ElementVisualizationLocalizations.GetString("restoreVisualization_transactionGroup", Value))
            : null;
        List<IRestoreSession> restoredSessions = [];
        List<Exception> errors = [];
        for (int index = _sessions.Count - 1; index >= 0; index--)
        {
            IRestoreSession session = _sessions[index];
            try
            {
                session.Restore();
            }
            catch (Autodesk.Revit.Exceptions.InvalidObjectException exception)
            {
                // The Revit objects this session compensates no longer exist: they were deleted, or
                // their creation was undone (for example the user undid the visualization view
                // creation). The state to restore went with them, so the session counts as restored
                // instead of failing identically on every retry.
                logger.LogWarning(
                    exception,
                    "Visualization session {SessionNumber} of pipeline {VisualizationName} for " +
                    "diagnostic {DiagnosticCode} refers to deleted objects; treating it as restored",
                    index + 1, Value, Identity.Code);
            }
            catch (Exception exception)
            {
                errors.Add(new InvalidOperationException(
                    $"Failed to restore visualization session {index + 1}.", exception));
                continue;
            }
            restoredSessions.Add(session);
        }
        Assimilate(transactionGroup);

        foreach (IRestoreSession session in restoredSessions)
        {
            _sessions.Remove(session);
            try
            {
                session.Dispose();
            }
            catch (Exception exception)
            {
                // A session classified as restored above keeps its delegate, so disposing it repeats
                // the same failure. Disposal is best-effort, as in Dispose(bool).
                logger.LogDebug(
                    exception,
                    "Failed to dispose a visualization session of pipeline {VisualizationName} for " +
                    "diagnostic {DiagnosticCode}",
                    Value, Identity.Code);
            }
        }

        if (errors.Count > 0)
        {
            AggregateException error = new(errors);
            // The first failure of a streak is unexpected; repeats of the same stuck sessions would
            // only spam the log (a poisoned API context or a permanently unrestorable session fails
            // every trigger identically), so they are warnings. A success or a new Apply starts
            // a new streak. The exception stays attached either way.
            _consecutiveRestoreFailures++;
            if (_consecutiveRestoreFailures == 1)
                logger.LogError(
                    error,
                    "Failed to restore {FailedSessionCount} visualization sessions in pipeline " +
                    "{VisualizationName} for diagnostic {DiagnosticCode}; sessions retained for retry",
                    errors.Count, Value, Identity.Code);
            else
                logger.LogWarning(
                    error,
                    "Still failing to restore {FailedSessionCount} visualization sessions in pipeline " +
                    "{VisualizationName} for diagnostic {DiagnosticCode}; sessions retained for retry",
                    errors.Count, Value, Identity.Code);
            throw new InvalidOperationException("Failed to restore the previous visualization state.", error);
        }

        _consecutiveRestoreFailures = 0;
        _sessionDocument = null;

        if (sessionCount > 0)
            logger.LogInformation(
                "Visualization pipeline {VisualizationName} for diagnostic {DiagnosticCode} restored",
                Value, Identity.Code);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Releases the visualization state owned by this pipeline.</summary>
    /// <param name="disposing"><see langword="true"/> when called from <see cref="Dispose()"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        _disposed = true;
        if (!disposing) return;

        try
        {
            Restore();
        }
        catch
        {
            // Disposal is best-effort. Explicit Restore reports failures to the caller.
        }
    }

    private IAccentElementsService ResolveService(AccentElementsType type) =>
        _services.TryGetValue(type, out IAccentElementsService? service)
            ? service
            : throw new InvalidOperationException($"Element accent service '{type}' is not registered.");

    private static ElementId[] ResolveElementIds(
        ElementVisualizationContext context,
        IReadOnlyList<string> elementSetKeys)
    {
        IEnumerable<IReadOnlyCollection<ElementId>> sets = elementSetKeys.Count == 0
            ? context.ElementSets.Values
            : elementSetKeys.Select(key => context.ElementSets.TryGetValue(key, out IReadOnlyCollection<ElementId>? set)
                ? set
                : throw new InvalidOperationException($"Visualization element set '{key}' was not provided."));

        return sets.SelectMany(set => set).Distinct().ToArray();
    }

    private static TransactionGroup? StartTransactionGroup(Document document, string name)
    {
        if (document.IsModifiable) return null;

        TransactionGroup group = new(document, name);
        if (group.Start() == TransactionStatus.Started) return group;

        group.Dispose();
        throw new InvalidOperationException($"Failed to start transaction group '{name}'.");
    }

    private static void Assimilate(TransactionGroup? group)
    {
        if (group is null) return;
        if (group.Assimilate() != TransactionStatus.Committed)
            throw new InvalidOperationException($"Failed to assimilate transaction group '{group.GetName()}'.");
    }

    private static void RollBack(TransactionGroup? group)
    {
        if (group is null || group.GetStatus() != TransactionStatus.Started) return;
        group.RollBack();
    }

    private static void RollBack(Transaction? transaction)
    {
        if (transaction is null) return;
        try
        {
            if (transaction.GetStatus() == TransactionStatus.Started)
                transaction.RollBack();
        }
        finally
        {
            transaction.Dispose();
        }
    }

    private static Exception? RestoreSessions(IEnumerable<IRestoreSession> sessions)
    {
        List<Exception> errors = [];
        foreach (IRestoreSession session in sessions.Reverse())
        {
            try
            {
                session.Dispose();
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }
        return errors.Count == 0 ? null : new AggregateException(errors);
    }

}
