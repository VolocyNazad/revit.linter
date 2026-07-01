namespace Revit.Linter.ParameterElementDiagnostics;

internal sealed class DocumentDiagnosticFilter(DocumentFilterFactory documentFilterFactory) : IDocumentDiagnosticFilter
{
    public required DocumentDiagnosticId Identity { get; init; }
    public required string Formula { get; init; }
    public bool IsRelevantFor(Document document) => FilterDelegate.Invoke(document);
    private Func<Document, bool> FilterDelegate => field ??= documentFilterFactory.Create(Formula);
}
