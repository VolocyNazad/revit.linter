using Autodesk.Revit.DB;
using Revit.Linter.ElementFixing.Abstractions.Models;

namespace Revit.Linter.ElementFixing.Abstractions.Services;

internal interface IElementFixStepExecutor
{
    ElementFixStepType Type { get; }

    bool Execute(Document document, IReadOnlyCollection<ElementId> elementIds);
}
