using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyInstanceMirrored;

internal sealed class FamilyInstanceMirroredDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilyInstanceMirrored;

    public bool IsRelevantFor(Document document, Element element) => element is FamilyInstance;
}
