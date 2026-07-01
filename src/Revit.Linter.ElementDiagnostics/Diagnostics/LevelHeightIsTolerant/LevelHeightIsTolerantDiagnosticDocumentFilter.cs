namespace Revit.Linter.ElementDiagnostics.Diagnostics.LevelHeightIsTolerant;

internal sealed class LevelHeightIsTolerantDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.LevelHeightIsTolerant;

    public bool IsRelevantFor(Document document) => !document.IsFamilyDocument;
}
