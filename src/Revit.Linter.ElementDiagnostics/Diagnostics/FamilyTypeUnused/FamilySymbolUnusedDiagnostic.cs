using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyTypeUnused;

internal sealed class FamilySymbolUnusedDiagnostic : IElementDiagnostic
{
    private readonly ElementFilter familyInstanceFilter = new ElementClassFilter(typeof(FamilyInstance));

    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilySymbolUnused;

    public DiagnosticResult Execute(Document document, View? view, Element targetElement)
    {
        var family = (FamilySymbol)targetElement;
        var instances = family.GetDependentElements(familyInstanceFilter);
        return instances.Any() ? new(DiagnosticVerdict.Valid) : new(DiagnosticVerdict.NotValid);
    }
}
