using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.ViewUnused;

internal sealed class ViewUnplacedDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ViewUnplaced;

    public bool IsRelevantFor(Document document, Element element)
        => element is View { IsTemplate: false, ViewType: not ViewType.DrawingSheet };
}