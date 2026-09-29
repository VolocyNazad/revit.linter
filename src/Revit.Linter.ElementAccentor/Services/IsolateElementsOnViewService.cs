using Microsoft.Extensions.Logging;
using Revit.Linter.ElementAccentor.Abstractions.Models;
using Revit.Linter.ElementAccentor.Abstractions.Services;

namespace Revit.Linter.ElementAccentor.Services;

internal sealed class IsolateElementsOnViewService(ILogger<IsolateElementsOnViewService> logger) : IAccentElementsService
{
    private readonly ILogger<IsolateElementsOnViewService> _logger = logger;

    public AccentElementsType Type => AccentElementsType.IsolateElementsOnView;

    public bool Execute(Document document, params ElementId[] elementIds)
        => Execute(document, document.ActiveView, elementIds);

    public IElementAccentSession Apply(
        Document document,
        View view,
        IReadOnlyCollection<ElementId> elementIds)
    {
        if (view.IsTemporaryHideIsolateActive())
            throw new InvalidOperationException(
                "Element isolation cannot replace an existing temporary hide/isolate state.");

        if (!Execute(document, view, elementIds.ToArray()))
            return new ElementAccentSession(static () => { });

        return new ElementAccentSession(() =>
        {
            if (document.IsValidObject && view.IsValidObject && view.IsTemporaryHideIsolateActive())
                TransactionExecutor.Execute(document, "Restore visualization isolation", () =>
                    view.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate));
        });
    }

    private bool Execute(
        Document document,
        View? activeView,
        IReadOnlyCollection<ElementId> elementIds)
    {
        if (!elementIds.Any())
        {
            _logger.LogInformation("Failed to get elements. The list of elements is empty.");
            return false;
        }
        if (activeView is null)
        {
            _logger.LogInformation("Active view not found.");
            return false;
        }
        if (!activeView.CanEnableTemporaryViewPropertiesMode())
        {
            _logger.LogInformation("Can't enable temporary view properties mode for view type {ViewType}.", activeView.ViewType);
            return false;
        }
        TransactionExecutor.Execute(document, "Apply visualization isolation", () =>
        {
            activeView.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);
            activeView.IsolateElementsTemporary(elementIds.ToArray());
        });

        _logger.LogInformation("Elements isolated.");

        return true;
    }
}
