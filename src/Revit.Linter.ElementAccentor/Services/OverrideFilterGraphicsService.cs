using Revit.Linter.ElementAccentor.Abstractions.Models;
using Revit.Linter.ElementAccentor.Abstractions.Services;

namespace Revit.Linter.ElementAccentor.Services;

internal sealed class OverrideFilterGraphicsService : IOverrideFilterGraphicsService
{
    public IRestoreSession Apply(
        Document document,
        View view,
        IReadOnlyCollection<ElementId> elementIds,
        ViewGraphicsStyle style)
    {
        ElementId filterId = ElementId.InvalidElementId;
        TransactionExecutor.Execute(document, ElementAccentorLocalizations.GetString("applyFilterGraphics_transaction"), () =>
        {
            SelectionFilterElement filter = SelectionFilterElement.Create(
                document, $"Revit Linter visualization {Guid.NewGuid():N}");
            filterId = filter.Id;
            filter.SetElementIds(elementIds.Distinct().ToArray());
            view.AddFilter(filterId);
            using OverrideGraphicSettings graphics = new();
            ViewGraphicsStyleApplicator.Apply(document, graphics, style);
            view.SetFilterOverrides(filterId, graphics);
        });

        return new RestoreSession(() =>
        {
            if (!document.IsValidObject || filterId == ElementId.InvalidElementId
                || document.GetElement(filterId) is null)
                return;
            TransactionExecutor.Execute(document, ElementAccentorLocalizations.GetString("restoreFilterGraphics_transaction"), () =>
                document.Delete(filterId));
        });
    }
}
