using Autodesk.Revit.DB;
using Revit.Linter.ElementFixing.Abstractions.Services;

namespace Revit.Linter.ElementFixing.Services;

internal sealed class DeleteElementFixStepExecutor : IElementFixStepExecutor
{
    public string Type => "Delete";

    public bool Execute(Document document, IReadOnlyCollection<ElementId> elementIds)
    {
        if (elementIds.Count == 0) return true;

        ICollection<ElementId> deletedIds = document.Delete(elementIds.ToArray());
        return elementIds.All(deletedIds.Contains);
    }
}
