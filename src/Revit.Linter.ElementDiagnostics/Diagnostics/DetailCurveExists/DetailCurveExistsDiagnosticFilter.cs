using Toolkit.Revit.Extensions;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.DetailCurveExists;

internal sealed class DetailCurveExistsDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.DetailCurveExists;

    public bool IsRelevantFor(Document document, Element element)
        => element is DetailCurve && element.Category != null
        && element.Category.BuiltInCategory == BuiltInCategory.OST_Lines;
}
