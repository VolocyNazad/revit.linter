using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.Core.Abstractions.Services;

public interface IDocumentDiagnosticFilter
{
    DocumentDiagnosticId Identity { get; }
    bool IsRelevantFor(Document document);
}
