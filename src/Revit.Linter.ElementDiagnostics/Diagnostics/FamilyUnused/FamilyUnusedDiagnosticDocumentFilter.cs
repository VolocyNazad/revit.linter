using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyUnused;

internal sealed class FamilyUnusedDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilyUnused;

    public bool IsRelevantFor(Document document) => !document.IsFamilyDocument;
}
