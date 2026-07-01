namespace Revit.Linter.ElementDiagnostics.Diagnostics.WallTopOffsetUnconnected;

internal sealed class WallTopOffsetUnconnectedDiagnostic : IElementDiagnostic
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.WallTopOffsetUnconnected;

    public DiagnosticResult Execute(Document document, View? view, Element targetElement)
    {
        var wall = (Wall)targetElement;
        return wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE)?.AsElementId() == ElementId.InvalidElementId
            ? new(DiagnosticVerdict.NotValid) : new(DiagnosticVerdict.Valid);
    }
}
