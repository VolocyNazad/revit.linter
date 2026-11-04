namespace Revit.Linter.ElementDiagnostics.Diagnostics.RevitLinkNotPinned;

internal sealed class RevitLinkNotPinnedDiagnostic : IElementDiagnostic
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.RevitLinkNotPinned;

    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
    {
        // An unloaded link keeps its instances, but there is no model content to move until the
        // link is reloaded, so such an instance is not a finding; the run after reloading reports
        // the pin state again.
        return targetElement is RevitLinkInstance { Pinned: false } link
               && link.GetLinkDocument() is not null
            ? new(DiagnosticVerdict.NotValid) : new(DiagnosticVerdict.Valid);
    }
}
