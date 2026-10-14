using Microsoft.Extensions.Logging;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.ElementAccentor.Abstractions.Models;
using Revit.Linter.ElementAccentor.Abstractions.Services;
using Revit.Linter.ElementVisualization.Abstractions.Models;
using Revit.Linter.ElementVisualization.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics;

/// <summary>Shows the finding target in the visualization view and selects it.</summary>
/// <remarks>For findings whose elements have model geometry. In family documents the presenter falls back to plain selection.</remarks>
internal abstract class ShowAndSelectVisualizationPipeline(
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
        [new AccentElementsStep(AccentElementsType.ShowElements, []),
            new AccentElementsStep(AccentElementsType.SelectElements, [])];
}
