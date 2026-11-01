using Microsoft.Extensions.Logging;
using Revit.Linter.ElementAccentor.Abstractions.Models;
using Revit.Linter.ElementAccentor.Abstractions.Services;

namespace Revit.Linter.ElementAccentor.Services;

internal sealed class ShowElementsService(ILogger<ShowElementsService> logger) : IAccentElementsService
{
    public AccentElementsType Type => AccentElementsType.ShowElements;

    public bool RunOnce(Document document, params ElementId[] elementIds)
    {
        new UIDocument(document).ShowElements(elementIds);

        logger.LogInformation("Elements showed.");

        return true;
    }

    public IRestoreSession Apply(
        Document document,
        View view,
        IReadOnlyCollection<ElementId> elementIds)
    {
        // The view can be deleted, or its creation undone, while the visualization is shown. Its
        // wrapper is invalid from that moment and even the Id getter throws InvalidObjectException,
        // so the session works from the captured id and never touches the View again.
        ElementId viewId = view.Id;
        using UIView? uiView = new UIDocument(document).GetOpenUIViews()
            .FirstOrDefault(candidate => candidate.ViewId.Equals(viewId));
        IList<XYZ>? previousCorners = uiView?.GetZoomCorners();
        if (uiView is null || !ZoomToElements(document, uiView, elementIds))
            return new RestoreSession(static () => { });

        return new RestoreSession(() =>
        {
            if (!document.IsValidObject || previousCorners is not { Count: 2 }) return;
            // A deleted view has no open window left, and a closed one has no window either;
            // without a window there is no zoom to put back.
            using UIView? currentView = new UIDocument(document).GetOpenUIViews()
                .FirstOrDefault(candidate => candidate.ViewId.Equals(viewId));
            if (currentView is null) return;
            try
            {
                currentView.ZoomAndCenterRectangle(previousCorners[0], previousCorners[1]);
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException)
            {
                // Returning to the previous zoom is a convenience, not part of the model state. Revit
                // rejects the saved corners when they no longer span a rectangle in the view, for example
                // when they were read from a view that had just been opened or the view was rotated
                // since. The view then keeps its current zoom and the rest of the visualization is
                // still restored.
            }
            catch (Autodesk.Revit.Exceptions.InvalidObjectException)
            {
                // The view window disappeared between the lookup and the zoom. The zoom is a
                // convenience like above, so the rest of the visualization is still restored.
            }
        });
    }

    // Frames the elements in the already open view instead of asking Revit to show them: picking
    // a view is what occasionally moved the visualization away from the visualization view.
    private bool ZoomToElements(
        Document document, UIView uiView, IReadOnlyCollection<ElementId> elementIds)
    {
        BoundingBoxXYZ? box = null;
        foreach (ElementId elementId in elementIds)
        {
            if (document.GetElement(elementId) is not Element element) continue;
            BoundingBoxXYZ? elementBox = element.get_BoundingBox(null);
            if (elementBox is null) continue;
            box = box is null ? elementBox : Union(box, elementBox);
        }

        if (box is null) return false;
        BoundingBoxXYZ framed = WithMargin(box);
        uiView.ZoomAndCenterRectangle(framed.Min, framed.Max);

        logger.LogInformation("Elements zoomed to fit in the open view.");
        return true;
    }

    private static BoundingBoxXYZ Union(BoundingBoxXYZ first, BoundingBoxXYZ second) => new()
    {
        Min = new XYZ(
            Math.Min(first.Min.X, second.Min.X),
            Math.Min(first.Min.Y, second.Min.Y),
            Math.Min(first.Min.Z, second.Min.Z)),
        Max = new XYZ(
            Math.Max(first.Max.X, second.Max.X),
            Math.Max(first.Max.Y, second.Max.Y),
            Math.Max(first.Max.Z, second.Max.Z)),
    };

    private static BoundingBoxXYZ WithMargin(BoundingBoxXYZ box)
    {
        const double marginRatio = 0.1;
        const double minimumMargin = 0.1;
        XYZ offset = new(
            Math.Max((box.Max.X - box.Min.X) * marginRatio, minimumMargin),
            Math.Max((box.Max.Y - box.Min.Y) * marginRatio, minimumMargin),
            Math.Max((box.Max.Z - box.Min.Z) * marginRatio, minimumMargin));
        return new BoundingBoxXYZ
        {
            Min = new XYZ(box.Min.X - offset.X, box.Min.Y - offset.Y, box.Min.Z - offset.Z),
            Max = new XYZ(box.Max.X + offset.X, box.Max.Y + offset.Y, box.Max.Z + offset.Z),
        };
    }
}
