using Microsoft.Extensions.Logging;
using Revit.Linter.ElementAccentor.Abstractions.Models;
using Revit.Linter.ElementAccentor.Abstractions.Services;

namespace Revit.Linter.ElementAccentor.Services;

internal sealed class IsolateElementsOnViewService(ILogger<IsolateElementsOnViewService> logger) : IAccentElementsService
{
    private readonly ILogger<IsolateElementsOnViewService> _logger = logger;

    public AccentElementsType Type => AccentElementsType.IsolateElementsOnView;

    public bool Execute(Document document, params ElementId[] elementIds)
    {
        if (!elementIds.Any())
        {
            _logger.LogInformation("Failed to get elements. The list of elements is empty.");
            return false;
        }
        View activeView = document.ActiveView;
        if (activeView is null)
        {
            _logger.LogInformation("Active view not found.");
            return false;
        }
        if (!activeView.CanEnableTemporaryViewPropertiesMode())
        {
            _logger.LogInformation("Can't enable temporary view properties mode for view type {viewType}.", activeView.ViewType);
            return false;
        }
        if (WorksharingUtils.GetCheckoutStatus(document, activeView.Id) == CheckoutStatus.OwnedByOtherUser)
        {
            _logger.LogInformation("Required worksets are not owned by the user.");
            return false;
        }

        activeView.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);
        activeView.IsolateElementsTemporary(elementIds);

        _logger.LogInformation("Elements isolated.");

        return true;
    }
}
