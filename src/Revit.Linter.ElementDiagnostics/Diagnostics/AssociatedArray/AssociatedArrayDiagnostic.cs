namespace Revit.Linter.ElementDiagnostics.Diagnostics.AssociatedArray;

internal sealed class AssociatedArrayDiagnostic : IElementDiagnostic
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.AssociatedArray;

    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
    {
        // Copies made without association leave no array element behind,
        // so any BaseArray is an associated (Group And Associate) array.
        return targetElement is BaseArray ? new(DiagnosticVerdict.NotValid) : new(DiagnosticVerdict.Valid);
    }
}
