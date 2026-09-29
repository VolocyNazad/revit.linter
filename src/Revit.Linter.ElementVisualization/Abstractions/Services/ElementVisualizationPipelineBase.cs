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
/// <param name="logger">Writes visualization lifecycle diagnostics.</param>
public abstract class ElementVisualizationPipelineBase(
    ElementDiagnosticId identity,
    string value,
    IEnumerable<IAccentElementsService> services,
    IOverrideElementGraphicsService overrideElementGraphicsService,
    IOverrideFilterGraphicsService overrideFilterGraphicsService,
    ILogger logger) : IElementVisualizationPipeline
{
    private readonly IReadOnlyDictionary<AccentElementsType, IAccentElementsService> _services =
        services.ToDictionary(service => service.Type);
    private readonly List<IElementAccentSession> _sessions = [];
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

        List<IElementAccentSession> appliedSessions = [];
        using TransactionGroup? transactionGroup = StartTransactionGroup(
            context.Document, $"Apply visualization: {Value}");
        try
        {
            for (int index = 0; index < Steps.Count; index++)
            {
                ElementVisualizationStep step = Steps[index];
                ElementId[] elementIds = ResolveElementIds(context, step.ElementSetKeys);
                logger.LogInformation(
                    "Applying visualization step {StepNumber}/{StepCount}: {StepType}. " +
                    "Element sets: {ElementSetKeys}; elements: {ElementCount}",
                    index + 1, Steps.Count, step.GetType().Name,
                    step.ElementSetKeys.Count == 0 ? "<all>" : string.Join(", ", step.ElementSetKeys),
                    elementIds.Length);
                IElementAccentSession session = step switch
                {
                    AccentElementsStep accent => ResolveService(accent.Type)
                        .Apply(context.Document, context.View, elementIds),
                    OverrideElementsStep graphics => overrideElementGraphicsService
                        .Apply(context.Document, context.View, elementIds, graphics.Style),
                    OverrideFilterStep filter => overrideFilterGraphicsService
                        .Apply(context.Document, context.View, elementIds, filter.Style),
                    _ => throw new InvalidOperationException(
                        $"Unsupported visualization step '{step.GetType().Name}'.")
                };
                appliedSessions.Add(session);
                logger.LogInformation(
                    "Visualization step {StepNumber}/{StepCount} completed: {StepType}",
                    index + 1, Steps.Count, step.GetType().Name);
            }

            Assimilate(transactionGroup);
            _sessions.AddRange(appliedSessions);
            _sessionDocument = context.Document;
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

    /// <inheritdoc />
    public void Restore()
    {
        int sessionCount = _sessions.Count;
        if (sessionCount > 0)
            logger.LogInformation(
                "Restoring visualization pipeline {VisualizationName} for diagnostic {DiagnosticCode}. " +
                "Sessions: {SessionCount}",
                Value, Identity.Code, sessionCount);

        using TransactionGroup? transactionGroup = _sessionDocument is null
            ? null
            : StartTransactionGroup(_sessionDocument, $"Restore visualization: {Value}");
        List<IElementAccentSession> restoredSessions = [];
        List<Exception> errors = [];
        for (int index = _sessions.Count - 1; index >= 0; index--)
        {
            IElementAccentSession session = _sessions[index];
            try
            {
                session.Restore();
                restoredSessions.Add(session);
            }
            catch (Exception exception)
            {
                errors.Add(new InvalidOperationException(
                    $"Failed to restore visualization session {index + 1}.", exception));
            }
        }
        Assimilate(transactionGroup);

        foreach (IElementAccentSession session in restoredSessions)
        {
            _sessions.Remove(session);
            session.Dispose();
        }

        if (errors.Count > 0)
        {
            AggregateException error = new(errors);
            logger.LogError(
                error,
                "Failed to restore {FailedSessionCount} visualization sessions in pipeline " +
                "{VisualizationName} for diagnostic {DiagnosticCode}; sessions retained for retry",
                errors.Count, Value, Identity.Code);
            throw new InvalidOperationException("Failed to restore the previous visualization state.", error);
        }

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

    private static Exception? RestoreSessions(IEnumerable<IElementAccentSession> sessions)
    {
        List<Exception> errors = [];
        foreach (IElementAccentSession session in sessions.Reverse())
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
