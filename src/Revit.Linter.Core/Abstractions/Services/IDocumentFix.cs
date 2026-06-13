using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.Core.Abstractions.Services;

public interface IDocumentFix
{
    ElementDiagnosticId Identity { get; }
    string Value { get; }
    bool Excecute(Document targetDocument);
}
