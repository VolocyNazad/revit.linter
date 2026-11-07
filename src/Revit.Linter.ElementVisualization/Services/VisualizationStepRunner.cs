using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.ElementAccentor.Abstractions.Models;
using Revit.Linter.ElementAccentor.Abstractions.Services;
using Revit.Linter.ElementVisualization.Abstractions.Models;

namespace Revit.Linter.ElementVisualization.Services;

/// <summary>Executes one pipeline step through the accelerator service that owns it.</summary>
/// <remarks>
/// The runner turns a declarative step into a reversible session: it resolves the element sets the
/// step names, dispatches to the operation, and reports how long the operation took. It does not
/// control transactions or the view, so every step behaves the same whether it runs before or after
/// the view is activated.
/// </remarks>
internal sealed class VisualizationStepRunner(
    IEnumerable<IAccentElementsService> accentServices,
    IOverrideElementGraphicsService overrideElementGraphicsService,
    IOverrideFilterGraphicsService overrideFilterGraphicsService,
    ISetCategoryVisibilityService setCategoryVisibilityService,
    IViewSettingsService viewSettingsService,
    ILogger logger)
{
    private readonly IReadOnlyDictionary<AccentElementsType, IAccentElementsService> _accentServices =
        accentServices.ToDictionary(service => service.Type);

    /// <summary>Runs one step and returns the session that restores what it changed.</summary>
    /// <param name="context">The document, view and element sets of the visualization.</param>
    /// <param name="step">The declarative step to execute.</param>
    /// <param name="stepNumber">The one-based position of the step in the pipeline, used by the log.</param>
    /// <param name="stepCount">The number of steps in the pipeline, used by the log.</param>
    public IRestoreSession Execute(
        ElementVisualizationContext context,
        ElementVisualizationStep step,
        int stepNumber,
        int stepCount)
    {
        ElementId[] elementIds = ResolveElementIds(context, step.ElementSetKeys);
        logger.LogInformation(
            "Applying visualization step {StepNumber}/{StepCount}: {StepType}. " +
            "Element sets: {ElementSetKeys}; elements: {ElementCount}",
            stepNumber, stepCount, step.GetType().Name,
            step.ElementSetKeys.Count == 0 ? "<all>" : string.Join(", ", step.ElementSetKeys),
            elementIds.Length);

        Stopwatch stopwatch = Stopwatch.StartNew();
        IRestoreSession session = ExecuteStep(context, step, elementIds);
        logger.LogInformation(
            "Visualization step {StepNumber}/{StepCount} completed: {StepType} in {ElapsedMilliseconds:F0} ms",
            stepNumber, stepCount, step.GetType().Name, stopwatch.Elapsed.TotalMilliseconds);
        return session;
    }

    private IRestoreSession ExecuteStep(
        ElementVisualizationContext context,
        ElementVisualizationStep step,
        ElementId[] elementIds)
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

    private IAccentElementsService ResolveService(AccentElementsType type) =>
        _accentServices.TryGetValue(type, out IAccentElementsService? service)
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
