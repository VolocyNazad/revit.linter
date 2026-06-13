using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.Core.Abstractions.Services;

public interface IElementDiagnostic
{
    ElementDiagnosticId Identity { get; }
    DiagnosticResult Execute(Document document, View? view, Element targetElement);
}