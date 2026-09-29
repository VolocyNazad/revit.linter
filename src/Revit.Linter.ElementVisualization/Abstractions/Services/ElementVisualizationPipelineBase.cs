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
                _sessions.Add(session);
                logger.LogInformation(
                    "Visualization step {StepNumber}/{StepCount} completed: {StepType}",
                    index + 1, Steps.Count, step.GetType().Name);
            }

            logger.LogInformation(
                "Visualization pipeline {VisualizationName} for diagnostic {DiagnosticCode} applied successfully",
                Value, Identity.Code);
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Visualization pipeline {VisualizationName} for diagnostic {DiagnosticCode} failed; " +
                "restoring {SessionCount} completed sessions",
                Value, Identity.Code, _sessions.Count);
            Restore();
            throw new InvalidOperationException(
                $"Visualization pipeline '{Value}' failed for diagnostic '{Identity.Code}'.",
                exception);
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

        Exception? error = null;
        for (int index = _sessions.Count - 1; index >= 0; index--)
        {
            try
            {
                _sessions[index].Restore();
            }
            catch (Exception exception)
            {
                error ??= exception;
                logger.LogError(
                    exception,
                    "Failed to restore visualization session {SessionNumber} in pipeline {VisualizationName} " +
                    "for diagnostic {DiagnosticCode}",
                    index + 1, Value, Identity.Code);
            }
            finally
            {
                _sessions[index].Dispose();
            }
        }
        _sessions.Clear();

        if (error is not null)
            throw new InvalidOperationException("Failed to restore the previous visualization state.", error);

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

}
