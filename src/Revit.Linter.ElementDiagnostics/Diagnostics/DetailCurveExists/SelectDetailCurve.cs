using Microsoft.Extensions.Logging;
using Revit.Linter.ElementAccentor.Abstractions.Services;
using Revit.Linter.ElementVisualization.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.DetailCurveExists;

internal sealed class SelectDetailCurve(
    IEnumerable<IAccentElementsService> services,
    IOverrideElementGraphicsService overrideElementGraphicsService,
    IOverrideFilterGraphicsService overrideFilterGraphicsService,
    ISetCategoryVisibilityService setCategoryVisibilityService,
    IViewSettingsService viewSettingsService,
    ILogger<SelectDetailCurve> logger)
    : SelectOnlyVisualizationPipeline(
        ElementDiagnosticIdCollector.DetailCurveExists,
        ElementDiagnosticLocalizations.GetString("select_visualization"),
        services,
        overrideElementGraphicsService,
        overrideFilterGraphicsService,
        setCategoryVisibilityService,
        viewSettingsService,
        logger);
