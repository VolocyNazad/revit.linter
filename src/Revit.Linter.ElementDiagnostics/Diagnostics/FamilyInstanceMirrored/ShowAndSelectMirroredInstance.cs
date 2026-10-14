using Microsoft.Extensions.Logging;
using Revit.Linter.ElementAccentor.Abstractions.Services;
using Revit.Linter.ElementVisualization.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyInstanceMirrored;

internal sealed class ShowAndSelectMirroredInstance(
    IEnumerable<IAccentElementsService> services,
    IOverrideElementGraphicsService overrideElementGraphicsService,
    IOverrideFilterGraphicsService overrideFilterGraphicsService,
    ISetCategoryVisibilityService setCategoryVisibilityService,
    IViewSettingsService viewSettingsService,
    ILogger<ShowAndSelectMirroredInstance> logger)
    : CutShowAndSelectVisualizationPipeline(
        ElementDiagnosticIdCollector.FamilyInstanceMirrored,
        ElementDiagnosticLocalizations.GetString("cut_show_and_select_visualization"),
        services,
        overrideElementGraphicsService,
        overrideFilterGraphicsService,
        setCategoryVisibilityService,
        viewSettingsService,
        logger);
