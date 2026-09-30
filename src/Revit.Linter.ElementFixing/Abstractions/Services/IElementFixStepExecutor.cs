using Autodesk.Revit.DB;

namespace Revit.Linter.ElementFixing.Abstractions.Services;

internal interface IElementFixStepExecutor
{
    string Type { get; }

    bool Execute(Document document, IReadOnlyCollection<ElementId> elementIds);
}
