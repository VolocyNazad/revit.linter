namespace Revit.Linter.ElementDiagnostics.Diagnostics.LocationLineElementWithTolerantLength;

internal sealed class LocationLineElementWithTolerantLengthDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.LocationLineElementWithTolerantLength;

    public bool IsRelevantFor(Document document) => !document.IsFamilyDocument;
}
