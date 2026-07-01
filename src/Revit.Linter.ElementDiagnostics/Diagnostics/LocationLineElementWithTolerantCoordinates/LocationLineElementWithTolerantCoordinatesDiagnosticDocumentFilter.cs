namespace Revit.Linter.ElementDiagnostics.Diagnostics.LocationLineElementWithTolerantCoordinates;

internal sealed class LocationLineElementWithTolerantCoordinatesDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.LocationLineElementWithTolerantCoordinates;

    public bool IsRelevantFor(Document document) => !document.IsFamilyDocument;
}
