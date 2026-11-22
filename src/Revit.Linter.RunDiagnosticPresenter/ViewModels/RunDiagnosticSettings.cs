using Toolkit.ValueStore.Abstractions;

namespace Revit.Linter.RunDiagnosticPresenter.ViewModels;

/// <summary>
/// Stores the user-selected diagnostic-run options and whether the report list follows the document.
/// </summary>
[StoreFile("settings.yml")]
public sealed class RunDiagnosticSettings
{
    /// <summary>
    /// Gets or sets whether diagnostics run only against the active view.
    /// </summary>
    public bool OnActiveViewMode { get; set; }

    /// <summary>
    /// Gets or sets whether the report list keeps its rows while the document changes.
    /// </summary>
    /// <remarks>
    /// A paused list is not updated when elements change; the state belongs to the pane tool bar and is
    /// kept across Revit sessions, so the user does not lose it in a restart.
    /// </remarks>
    public bool IsPaused { get; set; }

    // Kept for compatibility with settings.yml files created before Revit warnings
    // became a regular document diagnostic.
#pragma warning disable S1133 // Removing the legacy property would break deserialization of existing settings files.
    /// <summary>
    /// Gets or sets the legacy Revit-warning option retained for settings-file compatibility.
    /// </summary>
    [Obsolete("Revit warnings are configured through the RVT001 document diagnostic.")]
    public bool IncludeRevitWarnings { get; set; } = true;
#pragma warning restore S1133
}
