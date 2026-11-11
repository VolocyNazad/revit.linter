using Microsoft.Extensions.Logging;
using Revit.Linter.ElementAccentor.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.RenderAppearanceForShading;

internal sealed class SelectRenderAppearanceForShading(
    IEnumerable<IAccentElementsService> services,
    IOverrideElementGraphicsService overrideElementGraphicsService,
    IOverrideFilterGraphicsService overrideFilterGraphicsService,
    ISetCategoryVisibilityService setCategoryVisibilityService,
    IViewSettingsService viewSettingsService,
    ILogger<SelectRenderAppearanceForShading> logger)
    : SelectOnlyVisualizationPipeline(
        ElementDiagnosticIdCollector.RenderAppearanceForShading,
        ElementDiagnosticLocalizations.GetString("select_visualization"),
        services,
        overrideElementGraphicsService,
        overrideFilterGraphicsService,
        setCategoryVisibilityService,
        viewSettingsService,
        logger);
