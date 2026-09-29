using Microsoft.Extensions.Logging;
using Revit.Linter.ElementAccentor.Abstractions.Models;
using Revit.Linter.ElementAccentor.Abstractions.Services;
using Revit.Linter.ElementAccentor.Infrastructure.Extensions;

namespace Revit.Linter.ElementAccentor.Services;

internal sealed class CutViewByElementsService(ILogger<CutViewByElementsService> logger) : IAccentElementsService
{
    private readonly ILogger<CutViewByElementsService> _logger = logger;

    public double Offset { get; set; } = 500;
    public AccentElementsType Type { get; } = AccentElementsType.CutViewByElements;

    public bool Execute(Document document, params ElementId[] elementIds)
        => Execute(document, document.ActiveView, elementIds);

    public IElementAccentSession Apply(
        Document document,
        View view,
        IReadOnlyCollection<ElementId> elementIds)
    {
        if (view is not View3D view3D)
            return new ElementAccentSession(static () => { });

        bool wasActive = view3D.IsSectionBoxActive;
        BoundingBoxXYZ previousSectionBox = view3D.GetSectionBox();
        if (!Execute(document, view, elementIds.ToArray()))
        {
            previousSectionBox.Dispose();
            return new ElementAccentSession(static () => { });
        }

        return new ElementAccentSession(() =>
        {
            try
            {
                if (!document.IsValidObject || !view3D.IsValidObject) return;
                TransactionExecutor.Execute(document, "Restore visualization section box", () =>
                {
                    view3D.SetSectionBox(previousSectionBox);
                    view3D.IsSectionBoxActive = wasActive;
                });
            }
            finally
            {
                previousSectionBox.Dispose();
            }
        });
    }

    private bool Execute(Document document, View? activeView, IReadOnlyCollection<ElementId> elementIds)
    {
        if (!elementIds.Any())
        {
            _logger.LogInformation("Failed get elements. The list of elements is empty.");
            return false;
        }
        if (activeView is null)
        {
            _logger.LogInformation("Active view not found.");
            return false;
        }
        if (activeView is not View3D view3D)
        {
            _logger.LogInformation("It's impossible to crop the view. You need a 3D view.");
            return false;
        }

        ICollection<Element> elements = [.. elementIds
            .Select(document.GetElement).Where(i => i != null)];

        if (elements.Count < elementIds.Count)
        {
            _logger.LogInformation("Cut view failed. Not all elements exist in the model.");
            return false;
        }

        bool sectionBoxApplied = false;
        TransactionExecutor.Execute(document, "Apply visualization section box", () =>
        {
            sectionBoxApplied = view3D.SetSectionBoxBy(elements, Offset, Offset, Offset);
            if (sectionBoxApplied)
                view3D.IsSectionBoxActive = true;
        });

        if (!sectionBoxApplied)
        {
            _logger.LogInformation("Cut view failed. Elements do not have bounding boxes on the view.");
            return false;
        }

        _logger.LogInformation("View cut.");

        return true;
    }

}
