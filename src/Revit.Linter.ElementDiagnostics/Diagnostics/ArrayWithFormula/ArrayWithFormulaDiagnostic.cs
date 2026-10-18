namespace Revit.Linter.ElementDiagnostics.Diagnostics.ArrayWithFormula;

internal sealed class ArrayWithFormulaDiagnostic : IElementDiagnostic
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ArrayWithFormula;

    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
    {
        if (targetElement is not BaseArray array) return new(DiagnosticVerdict.Valid);
        // A label means the member count is driven by a family parameter (formula).
        return TryGetLabel(array) is not null ? new(DiagnosticVerdict.NotValid) : new(DiagnosticVerdict.Valid);
    }

    private static FamilyParameter? TryGetLabel(BaseArray array)
    {
        try
        {
            return array.Label;
        }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException)
        {
            // Reading Label on an unbound array throws a Revit InvalidOperationException
            // (verified live): an unreadable label counts as unbound, not as a run failure.
            // This is the Revit exception hierarchy, not System.
            return null;
        }
    }
}
