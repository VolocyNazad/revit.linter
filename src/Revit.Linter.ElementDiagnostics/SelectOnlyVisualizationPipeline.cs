using Microsoft.Extensions.Logging;
using Revit.Linter.ElementAccentor.Abstractions.Models;
using Revit.Linter.ElementAccentor.Abstractions.Services;
using Revit.Linter.ElementVisualization.Abstractions.Models;
using Revit.Linter.ElementVisualization.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics;

/// <summary>Selects the finding target without changing the view.</summary>
/// <remarks>For findings whose elements have no model geometry, where a show step would only move the view pointlessly.</remarks>
internal abstract class SelectOnlyVisualizationPipeline(
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
        [new AccentElementsStep(AccentElementsType.SelectElements, [])];
}
