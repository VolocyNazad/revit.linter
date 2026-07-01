using Revit.Linter.BackgroundDiagnostic.Abstractions.Services;

namespace Revit.Linter.BackgroundDiagnostic.Services;

internal sealed class UpdaterBuilder : IUpdaterBuilder
{
    private sealed class LambdaUpdater : IUpdater
    {
        public string AdditionalInformation { get; set; } = string.Empty;
        public ChangePriority ChangePriority { get; set; } = ChangePriority.Structure;
        public UpdaterId? UpdaterId { get; set; }
        public string UpdaterName { get; set; } = string.Empty;
        public Action<UpdaterData> Action { get; set; } = delegate { };

        public void Execute(UpdaterData data)
        {
            Action(data);
        }

        public string GetAdditionalInformation()
        {
            return AdditionalInformation;
        }

        public ChangePriority GetChangePriority()
        {
            return ChangePriority;
        }

        public UpdaterId? GetUpdaterId()
        {
            return UpdaterId;
        }

        public string GetUpdaterName()
        {
            return UpdaterName;
        }
    }

    private readonly LambdaUpdater _updater = new();

    public IUpdater Build() => _updater;

    public IUpdaterBuilder SetAdditionalInformation(string additioalInfo)
    {
        _updater.AdditionalInformation = additioalInfo;
        return this;
    }

    public IUpdaterBuilder SetChangePriority(ChangePriority changePriority)
    {
        _updater.ChangePriority = changePriority;
        return this;
    }

    public IUpdaterBuilder SetUpdaterId(UpdaterId updaterId)
    {
        _updater.UpdaterId = updaterId;
        return this;
    }

    public IUpdaterBuilder SetUpdaterName(string name)
    {
        _updater.UpdaterName = name;
        return this;
    }

    public IUpdaterBuilder SetAction(Action<UpdaterData> action)
    {
        _updater.Action = action;
        return this;
    }
}


