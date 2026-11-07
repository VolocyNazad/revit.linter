using Microsoft.Extensions.Logging;
using Revit.Linter.ElementAccentor.Abstractions.Models;
using Revit.Linter.ElementAccentor.Abstractions.Services;
using Revit.Linter.ElementVisualization.Abstractions.Models;
using Revit.Linter.ElementVisualization.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics;

/// <summary>Cuts the visualization view around the finding target, shows it and selects it.</summary>
/// <remarks>For findings whose elements have volumetric model geometry that can be hidden inside the model.</remarks>
internal abstract class CutShowAndSelectVisualizationPipeline(
    ElementDiagnosticId identity,
    string value,
    IEnumerable<IAccentElementsService> services,
    IOverrideElementGraphicsService overrideElementGraphicsService,
    IOverrideFilterGraphicsService overrideFilterGraphicsService,
    ISetCategoryVisibilityService setCategoryVisibilityService,
    IViewSettingsService viewSettingsService,
    ILogger logger)
    : ElementVisualizationPipelineBase(
        identity, value, services, overrideElementGraphicsService, overrideFilterGraphicsService,
        setCategoryVisibilityService, viewSettingsService, logger)
{
    protected override IReadOnlyList<ElementVisualizationStep> Steps { get; } =
        [HideScopeBoxesStep,
            new AccentElementsStep(AccentElementsType.CutViewByElements, []),
            new AccentElementsStep(AccentElementsType.ShowElements, []),
            new AccentElementsStep(AccentElementsType.SelectElements, [])];

    // Scope boxes only cover the framed elements in a visualization view.
    private static SetCategoryVisibilityStep HideScopeBoxesStep =>
        new([BuiltInCategory.OST_VolumeOfInterest], CategoryVisibilityMode.Hide, []);
}
