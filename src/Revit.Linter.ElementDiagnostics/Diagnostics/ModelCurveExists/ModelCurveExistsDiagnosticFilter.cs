#if BEFORE2024
using Revit.Sugar;
#endif

using Revit.Linter.ElementDiagnostics.Infrastructure.Extensions;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.ModelCurveExists;

internal sealed class ModelCurveExistsDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ModelCurveExists;

    public bool IsRelevantFor(Document document, Element element)
        => element is ModelCurve && element.Category != null 
        && element.Category.IsBuiltInCategory(BuiltInCategory.OST_Lines);
}
