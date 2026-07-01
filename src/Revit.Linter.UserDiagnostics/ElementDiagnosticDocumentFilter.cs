namespace Revit.Linter.UserDiagnostics;

internal sealed class ElementDiagnosticDocumentFilter(
    DocumentFilterFactory documentFilterFactory) : IElementDiagnosticDocumentFilter
{
    public required ElementDiagnosticId Identity { get; init; }
    public required string Formula { get; init; }
    public bool IsRelevantFor(Document document) => FilterDelegate.Invoke(document);
    private Func<Document, bool> FilterDelegate => field ??= documentFilterFactory.Create(Formula);
}
