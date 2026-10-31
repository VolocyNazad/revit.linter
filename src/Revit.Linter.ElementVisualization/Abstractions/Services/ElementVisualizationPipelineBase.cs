using Microsoft.Extensions.Logging;
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
        try
        {
            ElementId? activeViewIdBefore = context.Document.ActiveView?.Id;
            ApplySteps(context, appliedSessions);

            // If the active view changed while the steps ran (another view activated externally),
            // the steps above landed on the view the user has just left. They are undone and applied
            // again to the view the user is looking at now.
            View? activeViewAfter = context.Document.ActiveView;
            if (activeViewAfter is not null
                && !Equals(activeViewAfter.Id, activeViewIdBefore)
                && !Equals(activeViewAfter.Id, context.View.Id))
            {
                logger.LogInformation(
                    "Showing the elements moved the user from view {PreviousViewName} to view {ViewName}; " +
                    "applying visualization pipeline {VisualizationName} there",
                    context.View.Name, activeViewAfter.Name, Value);
                Exception? undoError = RestoreSessions(appliedSessions);
                appliedSessions.Clear();
                if (undoError is not null)
                    throw new InvalidOperationException(
                        "Failed to undo the visualization applied to the previous view.", undoError);

                ApplySteps(context with { View = activeViewAfter }, appliedSessions);
            }

            Assimilate(transactionGroup);
            _sessions.AddRange(appliedSessions);
            _sessionDocument = context.Document;
            _consecutiveRestoreFailures = 0;
            logger.LogInformation(
                "Visualization pipeline {VisualizationName} for diagnostic {DiagnosticCode} applied successfully",
                Value, Identity.Code);
            return true;
        }
        catch (Exception exception)
        {
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

    private void ApplySteps(ElementVisualizationContext context, List<IRestoreSession> appliedSessions)
    {
        for (int index = 0; index < Steps.Count; index++)
        {
            ElementVisualizationStep step = Steps[index];
            ElementId[] elementIds = ResolveElementIds(context, step.ElementSetKeys);
            LogStepApplying(index, step, elementIds);
            appliedSessions.Add(ExecuteStep(context, step, elementIds));
            LogStepApplied(index, step);
        }
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

    private void LogStepApplied(int index, ElementVisualizationStep step)
        => logger.LogInformation(
            "Visualization step {StepNumber}/{StepCount} completed: {StepType}",
            index + 1, Steps.Count, step.GetType().Name);

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
