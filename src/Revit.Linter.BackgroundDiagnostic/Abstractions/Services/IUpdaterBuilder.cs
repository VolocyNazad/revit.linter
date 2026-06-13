namespace Revit.Linter.BackgroundDiagnostic.Abstractions.Services;

internal interface IUpdaterBuilder
{
    IUpdater Build();
    IUpdaterBuilder SetUpdaterName(string name);
    IUpdaterBuilder SetUpdaterId(UpdaterId updaterId);
    IUpdaterBuilder SetChangePriority(ChangePriority changePriority);
    IUpdaterBuilder SetAdditionalInformation(string additioalInfo);
    IUpdaterBuilder SetAction(Action<UpdaterData> action);
}


