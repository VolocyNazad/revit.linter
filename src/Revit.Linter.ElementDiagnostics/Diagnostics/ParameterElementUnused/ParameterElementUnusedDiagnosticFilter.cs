using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.ParameterElementUnused;

internal sealed class ParameterElementUnusedDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ParameterElementUnused;

    public bool IsRelevantFor(Document document, Element element) => element is ParameterElement;
}