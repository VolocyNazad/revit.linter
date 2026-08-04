using Autodesk.Revit.DB;

namespace Toolkit.Revit.Extensions;

public sealed class UpdaterBuilder
{
    private sealed class LambdaUpdater : IUpdater
    {
        public string AdditionalInformation { get; set; } = string.Empty;
        public ChangePriority ChangePriority { get; set; } = ChangePriority.Structure;
        public UpdaterId? UpdaterId { get; set; }
        public string UpdaterName { get; set; } = string.Empty;
        public Action<UpdaterData> Action { get; set; } = delegate { };

        public void Execute(UpdaterData data) => Action(data);
        public string GetAdditionalInformation() => AdditionalInformation;
        public ChangePriority GetChangePriority() => ChangePriority;
        public UpdaterId? GetUpdaterId() => UpdaterId;
        public string GetUpdaterName() => UpdaterName;
    }

    private readonly LambdaUpdater _updater = new();

    public IUpdater Build() => _updater;

    public UpdaterBuilder SetAdditionalInformation(string additionalInformation)
    {
        _updater.AdditionalInformation = additionalInformation;
        return this;
    }

    public UpdaterBuilder SetChangePriority(ChangePriority changePriority)
    {
        _updater.ChangePriority = changePriority;
        return this;
    }

    public UpdaterBuilder SetUpdaterId(UpdaterId updaterId)
    {
        _updater.UpdaterId = updaterId;
        return this;
    }

    public UpdaterBuilder SetUpdaterName(string name)
    {
        _updater.UpdaterName = name;
        return this;
    }

    public UpdaterBuilder SetAction(Action<UpdaterData> action)
    {
        _updater.Action = action;
        return this;
    }
}
