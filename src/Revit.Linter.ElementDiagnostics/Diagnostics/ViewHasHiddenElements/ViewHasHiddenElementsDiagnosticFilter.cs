namespace Revit.Linter.ElementDiagnostics.Diagnostics.ViewHasHiddenElements;

internal sealed class ViewHasHiddenElementsDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ViewHasHiddenElements;

    public bool IsRelevantFor(Document document, Element element)
        => element is View { IsTemplate: false } view && IsGraphical(view.ViewType);

    // Only views that draw model or detail elements can carry an element-level Hide in View state.
    // Sheets, schedules, reports, browsers and walkthroughs cannot, and their view types are left
    // out instead of negative-checked, because the report view type names differ between versions.
    private static bool IsGraphical(ViewType viewType) => viewType is
        ViewType.FloorPlan or ViewType.CeilingPlan or ViewType.EngineeringPlan or ViewType.AreaPlan
        or ViewType.Section or ViewType.Elevation or ViewType.Detail or ViewType.ThreeD
        or ViewType.DraftingView or ViewType.Rendering;
}
