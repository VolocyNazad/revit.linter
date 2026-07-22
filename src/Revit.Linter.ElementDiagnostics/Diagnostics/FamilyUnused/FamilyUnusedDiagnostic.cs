using Toolkit.Revit.Extensions;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyUnused;

internal sealed class FamilyUnusedDiagnostic : IElementDiagnostic
{
    private readonly ElementFilter _filter = ElementFilterUtils.AllFilter();

    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilyUnused;

    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
    {
        var family = (Family)targetElement;
        var hasInstances = family.GetDependentElements(_filter)
            .Any(id =>
            {
                var element = document.GetElement(id);
                var typeId = element.GetTypeId();
                if (typeId is null || typeId == ElementId.InvalidElementId) return false;
                var type = document.GetElement(typeId);
                return (type as FamilySymbol)?.Family.Id == family.Id;
            });
        return hasInstances ? new(DiagnosticVerdict.Valid) : new(DiagnosticVerdict.NotValid);
    }
}
