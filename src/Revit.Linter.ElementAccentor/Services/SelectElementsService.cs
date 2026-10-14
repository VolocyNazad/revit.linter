using Microsoft.Extensions.Logging;
using Revit.Linter.ElementAccentor.Abstractions.Models;
using Revit.Linter.ElementAccentor.Abstractions.Services;

namespace Revit.Linter.ElementAccentor.Services;

internal sealed class SelectElementsService(ILogger<SelectElementsService> logger) : IAccentElementsService
{
    public AccentElementsType Type => AccentElementsType.SelectElements;

    public bool RunOnce(Document document, params ElementId[] elementIds)
    {
        new UIDocument(document).Selection.SetElementIds(elementIds);

        logger.LogInformation("Elements selected.");

        return true;
    }

    public IRestoreSession Apply(
        Document document,
        View view,
        IReadOnlyCollection<ElementId> elementIds)
    {
        UIDocument uiDocument = new(document);
        ElementId[] previousSelection = uiDocument.Selection.GetElementIds().ToArray();
        if (!RunOnce(document, elementIds.ToArray()))
            return new RestoreSession(static () => { });

        return new RestoreSession(() =>
        {
            if (document.IsValidObject)
                new UIDocument(document).Selection.SetElementIds(previousSelection);
        });
    }
}
