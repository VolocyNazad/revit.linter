using Revit.Linter.ElementAccentor.Abstractions.Models;
using Revit.Linter.ElementAccentor.Abstractions.Services;

namespace Revit.Linter.ElementAccentor.Services;

internal sealed class OverrideElementGraphicsService : IOverrideElementGraphicsService
{
    public IElementAccentSession Apply(
        Document document,
        View view,
        IReadOnlyCollection<ElementId> elementIds,
        ViewGraphicsStyle style)
    {
        Dictionary<ElementId, OverrideGraphicSettings> previous = elementIds.Distinct()
            .ToDictionary(elementId => elementId, view.GetElementOverrides);

        try
        {
            TransactionExecutor.Execute(document, "Apply diagnostic visualization", () =>
            {
                foreach (KeyValuePair<ElementId, OverrideGraphicSettings> entry in previous)
                {
                    using OverrideGraphicSettings graphics = new(entry.Value);
                    ViewGraphicsStyleApplicator.Apply(document, graphics, style);
                    view.SetElementOverrides(entry.Key, graphics);
                }
            });
        }
        catch
        {
            DisposeSettings(previous.Values);
            throw;
        }

        return new ElementAccentSession(() =>
        {
            try
            {
                if (!document.IsValidObject || !view.IsValidObject) return;
                TransactionExecutor.Execute(document, "Restore diagnostic visualization", () =>
                {
                    foreach (KeyValuePair<ElementId, OverrideGraphicSettings> entry in previous
                                 .Where(entry => document.GetElement(entry.Key) is not null))
                        view.SetElementOverrides(entry.Key, entry.Value);
                });
            }
            finally
            {
                DisposeSettings(previous.Values);
            }
        });
    }

    private static void DisposeSettings(IEnumerable<OverrideGraphicSettings> settings)
    {
        foreach (OverrideGraphicSettings setting in settings) setting.Dispose();
    }
}
