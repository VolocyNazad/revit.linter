using Microsoft.Extensions.Logging;
using Revit.Linter.ElementAccentor.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.ImportInstanceExists;

internal sealed class ShowAndSelectImportInstance(
    IEnumerable<IAccentElementsService> services,
    IOverrideElementGraphicsService overrideElementGraphicsService,
    IOverrideFilterGraphicsService overrideFilterGraphicsService,
    ISetCategoryVisibilityService setCategoryVisibilityService,
    IViewSettingsService viewSettingsService,
    ILogger<ShowAndSelectImportInstance> logger)
    : CutShowAndSelectVisualizationPipeline(
        ElementDiagnosticIdCollector.ImportInstanceExists,
        ElementDiagnosticLocalizations.GetString("cut_show_and_select_visualization"),
        services,
        overrideElementGraphicsService,
        overrideFilterGraphicsService,
        setCategoryVisibilityService,
        viewSettingsService,
        logger);
