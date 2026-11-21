namespace Revit.Linter.ElementDiagnostics.Diagnostics.WallWithMultipleLayers;

internal sealed class WallWithMultipleLayersDiagnostic : IElementDiagnostic
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.WallWithMultipleLayers;

    public IReadOnlyList<DiagnosticFeedback> Execute(Document document, View? view, Element targetElement)
    {
        if (targetElement is not Wall wall) return DiagnosticFeedbacks.Valid;
        try
        {
            CompoundStructure? compoundStructure = wall.WallType.GetCompoundStructure();
            return DiagnosticFeedbacks.Of(compoundStructure is { LayerCount: > 1 }
                ? new(DiagnosticVerdict.NotValid)
                : new(DiagnosticVerdict.Valid));
        }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException)
        {
            // Unreadable wall type state is clean, not a run failure. The Revit exception
            // hierarchy is its own type, so it is caught explicitly instead of System.InvalidOperationException.
            return DiagnosticFeedbacks.Valid;
        }
    }
}
