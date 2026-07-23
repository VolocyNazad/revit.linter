#if BEFORE2024
using Toolkit.Revit.Extensions;
#endif

namespace Revit.Linter.ElementDiagnostics.Diagnostics.ModelCurveExists;

internal sealed class ModelCurveExistsDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ModelCurveExists;

    public bool IsRelevantFor(Document document, Element element)
        => element is ModelCurve && element.Category != null 
        && element.Category.BuiltInCategory == BuiltInCategory.OST_Lines;
}
