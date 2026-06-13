using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyUnused;

internal sealed class DeteteUnusedFamily : IElementFix
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilyUnused;

    public string Value => "Удалить неиспользуемое семейство.";
    public bool Excecute(Element targetElement)
        => targetElement.Document.Delete(targetElement.Id).Any();
}
