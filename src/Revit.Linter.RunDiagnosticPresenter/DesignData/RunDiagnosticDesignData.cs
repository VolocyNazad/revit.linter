#pragma warning disable S2325 // The XAML designer binds to instance members only, so design properties stay non-static.

using CommunityToolkit.Mvvm.Input;
using Revit.Linter.Localization;
using System.Globalization;
using System.Windows.Input;

namespace Revit.Linter.RunDiagnosticPresenter.DesignData;

/// <summary>Provides representative sample values for <see cref="Views.RunDiagnosticView"/> in the XAML designer.</summary>
internal sealed class RunDiagnosticDesignData
{
    private const string ResourceBaseName = "Revit.Linter.Localization.RunDiagnosticPresenter.ViewModels.RunDiagnosticViewModel";

    /// <summary>Gets the localized pause button text.</summary>
    public string PauseTitle => Localized("pause_title");

    /// <summary>Gets the localized pause tooltip description.</summary>
    public string PauseDescription => Localized("pause_description");

    /// <summary>Gets the localized run button text.</summary>
    public string RunTitle => Localized("run_title");

    /// <summary>Gets the localized run tooltip description.</summary>
    public string RunDescription => Localized("run_description");

    /// <summary>Gets the localized duration of the most recent diagnostic run.</summary>
    public string DiagnosticTime => string.Format(
        CultureInfo.CurrentCulture,
        Localized("diagnosticDuration_text"),
        1.35);

    /// <summary>Gets the localized active-view option text.</summary>
    public string OnActiveViewText => Localized("onActiveView_text");

    /// <summary>Gets the localized active-view option tooltip description.</summary>
    public string OnActiveViewDescription => Localized("onActiveView_description");

    /// <summary>Gets or sets whether diagnostics are restricted to the active view.</summary>
    public bool OnActiveViewMode { get; set; }

    /// <summary>Gets the command that initializes the view when it is loaded.</summary>
    public ICommand InitializeCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the command that runs the enabled diagnostics.</summary>
    public ICommand RunDiagnosticCommand { get; } = new RelayCommand(() => { });

    private static string Localized(string key) => LocalizationResourceReader.GetString(ResourceBaseName, key);
}
