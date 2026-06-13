using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.Core.Abstractions.Services;

public interface IDiagnosticService
{
    DiagnosticServiceResult Excecute(Document document, IEnumerable<ElementId> elementIds, View? view = null);
    DiagnosticServiceResult Excecute(Document document, View? view = null);
}