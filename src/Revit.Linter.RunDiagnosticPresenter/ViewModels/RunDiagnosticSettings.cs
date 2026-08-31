using Toolkit.ValueStore.Abstractions;

namespace Revit.Linter.RunDiagnosticPresenter.ViewModels;

[StoreFile("settings.yml")]
internal sealed class RunDiagnosticSettings
{
    public bool OnActiveViewMode { get; set; }

    // Kept for compatibility with settings.yml files created before Revit warnings
    // became a regular document diagnostic.
    [Obsolete("Revit warnings are configured through the RVT document diagnostic.")]
    public bool IncludeRevitWarnings { get; set; } = true;
}
