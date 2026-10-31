using Revit.Linter.ElementAccentor.Abstractions.Models;
using Revit.Linter.ElementAccentor.Abstractions.Services;

namespace Revit.Linter.ElementAccentor.Services;

internal sealed class OverrideElementGraphicsService : IOverrideElementGraphicsService
{
    public IRestoreSession Apply(
        Document document,
        View view,
        IReadOnlyCollection<ElementId> elementIds,
        ViewGraphicsStyle style)
    {
        Dictionary<ElementId, OverrideGraphicSettings> previous = elementIds.Distinct()
            .ToDictionary(elementId => elementId, view.GetElementOverrides);
        Dictionary<ElementId, OverrideGraphicSettings>? pending = previous;

        try
        {
            TransactionExecutor.Execute(document, ElementAccentorLocalizations.GetString("applyElementGraphics_transaction"), () =>
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
            pending = null;
            throw;
        }

        return new RestoreSession(() =>
        {
            Dictionary<ElementId, OverrideGraphicSettings>? pendingSettings = pending;
            if (pendingSettings is null) return;
            // The overrides are disposable and are released only when the restore is over: a
            // retained session must still be able to retry a failure that was not the loss of the
            // objects themselves.
            if (!document.IsValidObject || !view.IsValidObject)
            {
                DisposeSettings(pendingSettings.Values);
                pending = null;
                return;
            }

            Dictionary<ElementId, OverrideGraphicSettings> settings = pendingSettings;
            TransactionExecutor.Execute(document, ElementAccentorLocalizations.GetString("restoreElementGraphics_transaction"), () =>
            {
                foreach (KeyValuePair<ElementId, OverrideGraphicSettings> entry in settings
                             .Where(entry => document.GetElement(entry.Key) is not null))
                    view.SetElementOverrides(entry.Key, entry.Value);
            });

            DisposeSettings(settings.Values);
            pending = null;
        });
    }

    private static void DisposeSettings(IEnumerable<OverrideGraphicSettings> settings)
    {
        foreach (OverrideGraphicSettings setting in settings) setting.Dispose();
    }
}
