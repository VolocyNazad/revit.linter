namespace Revit.Linter.UserDiagnostics;

internal sealed class ElementDiagnosticFilter(
    ElementFilterFactory elementFilterFactory) : IElementDiagnosticFilter
{
    public required ElementDiagnosticId Identity { get; init; }
    public required string Formula { get; init; }
    public bool IsRelevantFor(Document document, Element element) => Filter.PassesFilter(element);
    private ElementFilter Filter => field ??= elementFilterFactory.Create(Formula);
}
