using Microsoft.Extensions.Logging;
using Revit.Linter.ElementAccentor.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.DuplicateMaterial;

internal sealed class SelectDuplicateMaterial(
    IEnumerable<IAccentElementsService> services,
    IOverrideElementGraphicsService overrideElementGraphicsService,
    IOverrideFilterGraphicsService overrideFilterGraphicsService,
    ISetCategoryVisibilityService setCategoryVisibilityService,
    IViewSettingsService viewSettingsService,
    ILogger<SelectDuplicateMaterial> logger)
    : SelectOnlyVisualizationPipeline(
        ElementDiagnosticIdCollector.DuplicateMaterial,
        ElementDiagnosticLocalizations.GetString("select_visualization"),
        services,
        overrideElementGraphicsService,
        overrideFilterGraphicsService,
        setCategoryVisibilityService,
        viewSettingsService,
        logger);
