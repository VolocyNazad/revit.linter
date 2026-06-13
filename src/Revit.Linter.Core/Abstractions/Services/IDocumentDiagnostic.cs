using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.Core.Abstractions.Services;

public interface IDocumentDiagnostic
{
    DocumentDiagnosticId Identity { get; }
    DiagnosticResult Execute(Document targetDocument);
}
