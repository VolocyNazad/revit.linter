using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyTypeUnused;

internal sealed class FamilySymbolUnusedDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilySymbolUnused;

    public bool IsRelevantFor(Document document) => !document.IsFamilyDocument;
}
