using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyUnused;

internal sealed class FamilyUnusedDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilyUnused;

    public bool IsRelevantFor(Document document, Element element) 
        => element is Family family
        // todo С профилсями проблемы (не понятно как проверять их использование. Например в импостах витражей)
        && family.FamilyCategory?.Id.IntegerValue != (int)BuiltInCategory.OST_ProfileFamilies;
}
