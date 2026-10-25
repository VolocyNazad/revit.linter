using Microsoft.Extensions.Logging;
using Revit.Linter.ElementAccentor.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.LocationLineElementWithTolerantLength;

internal sealed class ShowAndSelectLocationLineElement(
    IEnumerable<IAccentElementsService> services,
    IOverrideElementGraphicsService overrideElementGraphicsService,
    IOverrideFilterGraphicsService overrideFilterGraphicsService,
    ISetCategoryVisibilityService setCategoryVisibilityService,
    IViewSettingsService viewSettingsService,
    ILogger<ShowAndSelectLocationLineElement> logger)
    : CutShowAndSelectVisualizationPipeline(
        ElementDiagnosticIdCollector.LocationLineElementWithTolerantLength,
        ElementDiagnosticLocalizations.GetString("cut_show_and_select_visualization"),
        services,
        overrideElementGraphicsService,
        overrideFilterGraphicsService,
        setCategoryVisibilityService,
        viewSettingsService,
        logger);
