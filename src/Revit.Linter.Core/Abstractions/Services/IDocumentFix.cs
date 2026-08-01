using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.Core.Abstractions.Services;

public interface IDocumentFix
{
    DocumentDiagnosticId Identity { get; }
    string Value { get; }
    bool Execute(Document targetDocument);
}
