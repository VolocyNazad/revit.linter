using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyUnused;

internal sealed class FamilyUnusedDiagnostic : IElementDiagnostic
{
    private readonly ElementFilter familyInstanceFilter = new ElementClassFilter(typeof(FamilyInstance));

    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilyUnused;

    public DiagnosticResult Execute(Document document, View? view, Element targetElement)
    {
        var family = (Family)targetElement;
        var instances = family.GetDependentElements(familyInstanceFilter);
        return instances.Any() ? new(DiagnosticVerdict.Valid) : new(DiagnosticVerdict.NotValid);
    }
}
