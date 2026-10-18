namespace Revit.Linter.ElementDiagnostics.Diagnostics.LargeArray;

internal sealed class LargeArrayDiagnostic : IElementDiagnostic
{
    private const int ProjectThreshold = 100;
    private const int FamilyThreshold = 50;

    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.LargeArray;

    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
    {
        if (targetElement is not BaseArray array) return new(DiagnosticVerdict.Valid);
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
