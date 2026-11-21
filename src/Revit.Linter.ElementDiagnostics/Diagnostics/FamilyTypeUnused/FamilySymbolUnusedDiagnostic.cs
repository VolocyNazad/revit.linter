using Revit.Sugar;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyTypeUnused;

internal sealed class FamilySymbolUnusedDiagnostic : IElementDiagnostic
{
    private readonly ElementFilter _filter = ElementFilterUtils.AllFilter();

    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilySymbolUnused;

    public IReadOnlyList<DiagnosticFeedback> Execute(Document document, View? view, Element targetElement)
    {
        var familySymbol = (FamilySymbol)targetElement;
        var hasInstances = familySymbol.GetDependentElements(_filter)
            .Any(id => document.GetElement(id).GetTypeId() == familySymbol.Id);
        return DiagnosticFeedbacks.Of(hasInstances ? new(DiagnosticVerdict.Valid) : new(DiagnosticVerdict.NotValid));
    }
}
