#if BEFORE2024
using Revit.Sugar;
#endif

using Revit.Linter.ElementDiagnostics.Infrastructure.Extensions;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.DetailCurveExists;

internal sealed class DetailCurveExistsDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.DetailCurveExists;

    public bool IsRelevantFor(Document document, Element element)
        => element is DetailCurve && element.Category != null
        && element.Category.IsBuiltInCategory(BuiltInCategory.OST_Lines);
}
