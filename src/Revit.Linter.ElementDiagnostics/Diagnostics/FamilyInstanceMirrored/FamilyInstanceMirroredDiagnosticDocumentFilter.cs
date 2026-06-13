using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyInstanceMirrored;

internal sealed class FamilyInstanceMirroredDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilyInstanceMirrored;

    public bool IsRelevantFor(Document document) => !document.IsFamilyDocument;
}
