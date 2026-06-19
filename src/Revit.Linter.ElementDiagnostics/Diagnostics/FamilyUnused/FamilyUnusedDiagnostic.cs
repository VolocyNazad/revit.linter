using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyUnused;

internal sealed class FamilyUnusedDiagnostic : IElementDiagnostic
{
    private readonly ElementFilter filter = new LogicalOrFilter(
       new ElementIsElementTypeFilter(true),
       new ElementIsElementTypeFilter(false)
   );

    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilyUnused;

    public DiagnosticResult Execute(Document document, View? view, Element targetElement)
    {
        var family = (Family)targetElement;
        var hasInstances = family.GetDependentElements(filter)
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
