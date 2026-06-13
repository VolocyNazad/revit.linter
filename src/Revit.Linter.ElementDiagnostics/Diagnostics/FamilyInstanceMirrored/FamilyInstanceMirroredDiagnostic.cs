using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyInstanceMirrored;

internal sealed class FamilyInstanceMirroredDiagnostic : IElementDiagnostic
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilyInstanceMirrored;

    public DiagnosticResult Execute(Document document, View? view, Element targetElement)
    {
        var familyInstance = (FamilyInstance)targetElement;
        return familyInstance.Mirrored ? new(DiagnosticVerdict.NotValid) : new(DiagnosticVerdict.Valid);
    }
}
