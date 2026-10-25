using Microsoft.Extensions.Logging;
using Revit.Linter.ElementAccentor.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.AllConnectorsConnected;

internal sealed class ShowAndSelectNotConnectedElement(
    IEnumerable<IAccentElementsService> services,
    IOverrideElementGraphicsService overrideElementGraphicsService,
    IOverrideFilterGraphicsService overrideFilterGraphicsService,
    ISetCategoryVisibilityService setCategoryVisibilityService,
    IViewSettingsService viewSettingsService,
    ILogger<ShowAndSelectNotConnectedElement> logger)
    : CutShowAndSelectVisualizationPipeline(
        ElementDiagnosticIdCollector.AnyConnectorsNotConnected,
        ElementDiagnosticLocalizations.GetString("cut_show_and_select_visualization"),
        services,
        overrideElementGraphicsService,
        overrideFilterGraphicsService,
        setCategoryVisibilityService,
        viewSettingsService,
        logger);
