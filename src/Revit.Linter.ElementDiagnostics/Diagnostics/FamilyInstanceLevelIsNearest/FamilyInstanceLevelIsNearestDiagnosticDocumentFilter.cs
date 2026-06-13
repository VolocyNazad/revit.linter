using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyInstanceLevelIsNearest;

internal sealed class FamilyInstanceLevelIsNearestDiagnosticDocumentFilter : IElementDiagnosticDocumentFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilyInstanceLevelIsNearest;

    public bool IsRelevantFor(Document document) => !document.IsFamilyDocument;
}
