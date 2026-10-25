#pragma warning disable S2325 // The XAML designer binds to instance members only, so design properties stay non-static.
using CommunityToolkit.Mvvm.Input;
using Revit.Linter.Localization;
using System.Windows.Input;

namespace Revit.Linter.PracticalTourPresenter.DesignData;

/// <summary>Provides representative sample values for <see cref="Views.PracticalTourView"/> in the XAML designer.</summary>
internal sealed class PracticalTourDesignData
{
    private const string ResourceBaseName = "Revit.Linter.Localization.PracticalTourPresenter.ViewModels.PracticalTourViewModel";

    /// <summary>Gets the sample tour title.</summary>
    public string TitleText => LocalizationResourceReader.GetString(ResourceBaseName, "title_text");

    /// <summary>Gets the sample tour steps; the configuration-folder step is the current one.</summary>
    public IReadOnlyList<PracticalTourStepDesignData> Steps { get; } =
    [
        new(GetString("openDocumentStep_text"), GetString("openDocument_text"), false, true, false),
        new(GetString("openConfigurationFolderStep_text"), GetString("openConfigurationFolder_text"), true, false, false),
        new(GetString("searchAndFiltersStep_text"), GetString("searchAndFilters_text"), false, false, false),
        new(GetString("selectDiagnosticStep_text"), GetString("selectDiagnostic_text"), false, false, false),
        new(GetString("runDiagnosticsStep_text"), GetString("runDiagnostics_text"), false, false, false),
        new(GetString("inspectFindingStep_text"), GetString("inspectFinding_text"), false, false, false),
        new(GetString("openHelpStep_text"), GetString("openHelp_text"), false, false, false),
        new(GetString("showElementStep_text"), GetString("showElement_text"), false, false, false),
        new(GetString("selectVisualizationStep_text"), GetString("selectVisualization_text"), false, false, false),
        new(GetString("navigateFindingsStep_text"), GetString("navigateFindings_text"), false, false, false),
        new(GetString("inspectFixesStep_text"), GetString("understandFix_text"), false, false, false),
        new(GetString("fixListStep_text"), GetString("fixList_text"), false, false, false),
        new(GetString("understandIgnoreStep_text"), GetString("understandIgnore_text"), false, false, false),
        new(GetString("ignoreListStep_text"), GetString("ignoreList_text"), false, false, false),
        new(GetString("exportReportStep_text"), GetString("exportReport_text"), false, false, false),
        new(GetString("exportReportAnotherFormatStep_text"), GetString("exportReportAnotherFormat_text"), false, false, false),
    ];

    /// <summary>Gets the localized stop button text.</summary>
    public string StopText => LocalizationResourceReader.GetString(ResourceBaseName, "stop_text");

    /// <summary>Gets the localized restart button text.</summary>
    public string RestartText => LocalizationResourceReader.GetString(ResourceBaseName, "restart_text");

    /// <summary>Gets the localized opt-out button text.</summary>
    public string OptOutText => LocalizationResourceReader.GetString(ResourceBaseName, "optOut_text");

    /// <summary>Gets the command that pauses the practical tour; it does nothing in the designer.</summary>
    public ICommand StopCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the command that restarts the practical tour; it does nothing in the designer.</summary>
    public ICommand RestartCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the command that disables the practical tour; it does nothing in the designer.</summary>
    public ICommand OptOutCommand { get; } = new RelayCommand(() => { });

    private static string GetString(string key) => LocalizationResourceReader.GetString(ResourceBaseName, key);
}

/// <summary>Describes one sample row of the practical tour.</summary>
/// <param name="Title">The step title shown in the tour list.</param>
/// <param name="Instruction">The instruction of the current step.</param>
/// <param name="IsCurrent">A value indicating whether the step is the current one.</param>
/// <param name="IsCompleted">A value indicating whether the step has been completed.</param>
/// <param name="IsUnavailable">A value indicating whether the step cannot be completed in the current run.</param>
internal sealed record PracticalTourStepDesignData(
    string Title,
    string Instruction,
    bool IsCurrent,
    bool IsCompleted,
    bool IsUnavailable);
