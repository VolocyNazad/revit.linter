using Microsoft.Extensions.Logging;
using Revit.Linter.ElementAccentor.Abstractions.Models;
using Revit.Linter.ElementAccentor.Abstractions.Services;

namespace Revit.Linter.ElementAccentor.Services;

internal sealed class ShowElementsService(ILogger<ShowElementsService> logger) : IAccentElementsService
{
    public AccentElementsType Type => AccentElementsType.ShowElements;

    public bool Execute(Document document, params ElementId[] elementIds)
    {
        new UIDocument(document).ShowElements(elementIds);

        logger.LogInformation("Elements showed.");

        return true;
    }

    public IElementAccentSession Apply(
        Document document,
        View view,
        IReadOnlyCollection<ElementId> elementIds)
    {
        UIView? uiView = new UIDocument(document).GetOpenUIViews()
            .FirstOrDefault(candidate => candidate.ViewId == view.Id);
        IList<XYZ>? previousCorners = uiView?.GetZoomCorners();
        if (!Execute(document, elementIds.ToArray()))
            return new ElementAccentSession(static () => { });

        return new ElementAccentSession(() =>
        {
            if (!document.IsValidObject || previousCorners is not { Count: 2 }) return;
            using UIView? currentView = new UIDocument(document).GetOpenUIViews()
                .FirstOrDefault(candidate => candidate.ViewId == view.Id);
            currentView?.ZoomAndCenterRectangle(previousCorners[0], previousCorners[1]);
        });
    }
}
