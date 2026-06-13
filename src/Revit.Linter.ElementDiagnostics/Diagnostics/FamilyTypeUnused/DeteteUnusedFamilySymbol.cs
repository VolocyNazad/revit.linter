using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyTypeUnused;

internal sealed class DeteteUnusedFamilySymbol : IElementFix
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilySymbolUnused;

    public string Value => "Удалить неиспользуемый типоразмер семейства.";
    public bool Excecute(Element targetElement)
        => targetElement.Document.Delete(targetElement.Id).Any();
}
