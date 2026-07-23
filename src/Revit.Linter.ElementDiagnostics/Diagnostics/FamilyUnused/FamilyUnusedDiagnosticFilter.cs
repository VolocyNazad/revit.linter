using Toolkit.Revit.Extensions;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyUnused;

internal sealed class FamilyUnusedDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilyUnused;

    public bool IsRelevantFor(Document document, Element element) 
        => element is Family family
        // todo С профилями проблемы (не понятно как проверять их использование. Например в импостах витражей)
        && family.FamilyCategory?.Id.Value() != (int)BuiltInCategory.OST_ProfileFamilies;
}
