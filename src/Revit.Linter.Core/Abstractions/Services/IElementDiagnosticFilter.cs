using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.Core.Abstractions.Services;

public interface IElementDiagnosticFilter
{
    ElementDiagnosticId Identity { get; }
    bool IsRelevantFor(Document document, Element element);
}