using Toolkit.ValueStore.Abstractions;

namespace Revit.Linter.RunDiagnosticPresenter.ViewModels;

/// <summary>
/// Stores the user-selected scope for subsequent diagnostic runs.
/// </summary>
[StoreFile("settings.yml")]
public sealed class RunDiagnosticSettings
{
    /// <summary>
    /// Gets or sets whether diagnostics run only against the active view.
    /// </summary>
    public bool OnActiveViewMode { get; set; }

    // Kept for compatibility with settings.yml files created before Revit warnings
    // became a regular document diagnostic.
#pragma warning disable S1133 // Removing the legacy property would break deserialization of existing settings files.
    /// <summary>
    /// Gets or sets the legacy Revit-warning option retained for settings-file compatibility.
    /// </summary>
    [Obsolete("Revit warnings are configured through the RVT document diagnostic.")]
    public bool IncludeRevitWarnings { get; set; } = true;
#pragma warning restore S1133
}
