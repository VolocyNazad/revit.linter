using Autodesk.Revit.DB;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.ElementFixing.Abstractions.Models;
using Revit.Linter.ElementFixing.Abstractions.Services;

namespace Revit.Linter.ElementFixing.Services;

internal sealed class DeleteElementFixStepExecutor : IElementFixStepExecutor
{
    public ElementFixStepType Type => ElementFixStepType.Delete;

    public IElementFixStep Create(ElementFixStepDefinition step) => new DeleteElementFixStep();

    private sealed class DeleteElementFixStep : IElementFixStep
    {
        public bool Execute(Document document, IReadOnlyCollection<ElementId> elementIds)
        {
            if (elementIds.Count == 0) return true;

            ICollection<ElementId> deletedIds = document.Delete(elementIds.ToArray());
            return elementIds.All(deletedIds.Contains);
        }
    }
}