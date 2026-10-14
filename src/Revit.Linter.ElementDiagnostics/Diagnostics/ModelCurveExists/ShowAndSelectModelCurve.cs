using Microsoft.Extensions.Logging;
using Revit.Linter.ElementAccentor.Abstractions.Services;
using Revit.Linter.ElementVisualization.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.ModelCurveExists;

internal sealed class ShowAndSelectModelCurve(
    IEnumerable<IAccentElementsService> services,
    IOverrideElementGraphicsService overrideElementGraphicsService,
    IOverrideFilterGraphicsService overrideFilterGraphicsService,
    ISetCategoryVisibilityService setCategoryVisibilityService,
    IViewSettingsService viewSettingsService,
    ILogger<ShowAndSelectModelCurve> logger)
    : ShowAndSelectVisualizationPipeline(
        ElementDiagnosticIdCollector.ModelCurveExists,
        ElementDiagnosticLocalizations.GetString("show_and_select_visualization"),
        services,
        overrideElementGraphicsService,
        overrideFilterGraphicsService,
        setCategoryVisibilityService,
        viewSettingsService,
        logger);
