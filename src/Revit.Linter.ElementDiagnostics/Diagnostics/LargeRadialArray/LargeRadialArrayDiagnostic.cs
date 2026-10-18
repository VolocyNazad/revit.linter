namespace Revit.Linter.ElementDiagnostics.Diagnostics.LargeRadialArray;

internal sealed class LargeRadialArrayDiagnostic : IElementDiagnostic
{
    private const int ProjectThreshold = 50;
    private const int FamilyThreshold = 25;

    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.LargeRadialArray;

    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
    {
        if (targetElement is not RadialArray array) return new(DiagnosticVerdict.Valid);
        try
        {
            int threshold = document.IsFamilyDocument ? FamilyThreshold : ProjectThreshold;
            return array.NumMembers >= threshold ? new(DiagnosticVerdict.NotValid) : new(DiagnosticVerdict.Valid);
        }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException)
        {
            // Unreadable array state is clean, not a run failure. This is the Revit
            // exception hierarchy, not System: a BCL catch would miss it (verified live).
            return new(DiagnosticVerdict.Valid);
        }
    }
}
