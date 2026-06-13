using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyInstanceElevationIsTolerant;

internal sealed class FamilyInstanceElevationIsTolerantDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilyInstanceElevationIsTolerant;

    public bool IsRelevantFor(Document document) => !document.IsFamilyDocument;
}
