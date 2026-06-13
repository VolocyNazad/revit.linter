using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.LocationLineElementWithTolerantLength;

internal sealed class LocationLineElementWithTolerantLengthDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.LocationLineElementWithTolerantLength;

    public bool IsRelevantFor(Document document) => !document.IsFamilyDocument;
}
