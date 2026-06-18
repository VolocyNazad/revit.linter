using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyTypeUnused;

internal sealed class FamilySymbolUnusedDiagnostic : IElementDiagnostic
{
    private readonly ElementFilter filter = new LogicalOrFilter(
        new ElementIsElementTypeFilter(true), 
        new ElementIsElementTypeFilter(false)
    );

    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilySymbolUnused;

    public DiagnosticResult Execute(Document document, View? view, Element targetElement)
    {
        var familySymbol = (FamilySymbol)targetElement;
        var hasInstances = familySymbol.GetDependentElements(filter)
            .Any(id => document.GetElement(id).GetTypeId() == familySymbol.Id);
        return hasInstances ? new(DiagnosticVerdict.Valid) : new(DiagnosticVerdict.NotValid);
    }
}
