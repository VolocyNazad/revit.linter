namespace Revit.Linter.Diagnostic.Abstractions.Services;

public interface IDiagnosticService
{
    DiagnosticServiceResult Execute(Document document, IEnumerable<ElementId> elementIds, View? view = null);
    DiagnosticServiceResult Execute(Document document, View? view = null);
}