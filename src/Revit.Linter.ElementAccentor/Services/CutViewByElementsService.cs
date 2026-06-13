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
    {
        if (!elementIds.Any())
        {
            _logger.LogInformation("Failed get elements. The list of elements is empty.");
            return false;
        }
        View activeView = document.ActiveView;
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

        if (WorksharingUtils.GetCheckoutStatus(document, activeView.Id) == CheckoutStatus.OwnedByOtherUser)
        {
            _logger.LogInformation("Required worksets are not owned by the user.");
            return false;
        }
        ICollection<Element> elements = [.. elementIds
            .Select(document.GetElement).Where(i => i != null)];

        if (elements.Count < elementIds.Count())
        {
            _logger.LogInformation("Cut view failed. Not all elements exist in the model.");
            return false;
        }

        view3D.SetSectionBoxBy(elements, Offset, Offset, Offset);

        _logger.LogInformation("View cut.");

        return true;
    }

}
