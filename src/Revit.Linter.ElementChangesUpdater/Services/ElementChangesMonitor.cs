using Autodesk.Revit.DB;
using Revit.Context.Abstractions.Services;
using Revit.Linter.ElementChangesMonitor.Abstractions.Services;
using Revit.Linter.ElementChangesMonitor.Infrastructure.Builders;
using Revit.Linter.ElementChangesProvider.Abstractions.Services;
using Toolkit.Revit.Extensions;

namespace Revit.Linter.ElementChangesMonitor.Services;

internal sealed class ElementChangesMonitor(
    IRevitContext revitContext, IElementChangesSender elementChangesSender) : IElementChangesMonitor
{
    private static readonly ElementFilter _elementFilter = ElementFilterUtils.AllFilter();
    private static readonly ChangeType _changeType = Element.GetChangeTypeAny();

    private UpdaterId? UpdaterId = null;

    public bool Run()
    {
        var id = Guid.NewGuid();

        IUpdater updater = new UpdaterBuilder()
           .SetUpdaterId(new(revitContext.ControlledApplication!.ActiveAddInId, id))
           .SetChangePriority(ChangePriority.Structure)
           .SetUpdaterName("Element changes monitoring")
           .SetAdditionalInformation("Revit linter element changes monitoring")
           .SetAction(Handle).Build();

        UpdaterId updaterId = updater.GetUpdaterId();

        if (UpdaterRegistry.IsUpdaterRegistered(updaterId)) return false;

        UpdaterRegistry.RegisterUpdater(updater, true);
        UpdaterRegistry.AddTrigger(updaterId, _elementFilter, _changeType);

        UpdaterId = updaterId;

        return true;
    }

    public bool Stop()
    {
        if (UpdaterId is null) return false;

        if (!UpdaterRegistry.IsUpdaterRegistered(UpdaterId)) return false;

        UpdaterRegistry.UnregisterUpdater(UpdaterId);

        return true;
    }

    private void Handle(UpdaterData data)
        => elementChangesSender.Send(
            new(
                data.GetDocument(),
                data.GetAddedElementIds(),
                data.GetModifiedElementIds(),
                data.GetDeletedElementIds()));
}