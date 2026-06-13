using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.CollisionDiagnostics;

internal sealed class ElementDiagnosticDocumentFilter(
    DocumentFilterFactory documentFilterFactory) : IElementDiagnosticDocumentFilter
{
    public required ElementDiagnosticId Identity { get; init; }
    public required string Formula { get; init; }
    public bool IsRelevantFor(Document document) => FilterDelegate.Invoke(document);
    private Func<Document, bool> FilterDelegate => field ??= documentFilterFactory.Create(Formula);
}
