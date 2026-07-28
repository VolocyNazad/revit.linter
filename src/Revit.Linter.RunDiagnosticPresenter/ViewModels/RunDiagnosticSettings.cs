using Revit.Linter.ValueStore.Abstractions;

namespace Revit.Linter.RunDiagnosticPresenter.ViewModels;

[StoreFile("settings.yml")]
internal sealed class RunDiagnosticSettings
{
    public bool OnActiveViewMode { get; set; }
    public bool IncludeRevitWarnings { get; set; } = true;
}
