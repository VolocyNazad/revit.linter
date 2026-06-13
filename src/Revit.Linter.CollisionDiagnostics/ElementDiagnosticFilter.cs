using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.CollisionDiagnostics;

internal sealed class ElementDiagnosticFilter(
    ElementFilterFactory elementFilterFactory) : IElementDiagnosticFilter
{
    public required ElementDiagnosticId Identity { get; init; }
    public required string Formula { get; init; }
    public bool IsRelevantFor(Document document, Element element) => Filter.PassesFilter(element);
    private ElementFilter Filter => field ??= elementFilterFactory.Create(Formula);
}