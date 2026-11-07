using Revit.Linter.PracticalTourPresenter.Interactions.Abstractions.Models;

namespace Revit.Linter.PracticalTourPresenter.Infrastructure.Services;

/// <summary>Describes what the tour presents for one step.</summary>
/// <param name="Step">The step this definition describes.</param>
/// <param name="TitleKey">The resource key of the title shown in the step list, or <see langword="null"/> when the step has no row.</param>
/// <param name="InstructionKey">The resource key of the instruction shown for the step.</param>
/// <param name="Pane">The pane brought forward for the step, or <see langword="null"/> to keep the current pane.</param>
/// <param name="HighlightKey">The registered key of the spotlight target, or <see langword="null"/> when the step has no target.</param>
/// <param name="IsVisible">Whether the step appears in the visible step list.</param>
/// <param name="RequiresTrainingScope">Whether the catalog publishes only the training check during the step.</param>
/// <param name="NeedsFindings">Whether the step is unavailable after a clean run without findings.</param>
/// <param name="NeedsVisualizationChoice">Whether the step is unavailable when the finding offers a single visualization.</param>
/// <param name="UnavailableTitleKey">The title shown when the step is unavailable for the current session.</param>
/// <param name="CleanRunInstructionKey">The instruction shown after a clean run without findings.</param>
/// <param name="TutorialSampleInstructionKey">The instruction shown while the tutorial sample is about to open.</param>
/// <param name="CheckpointInstructionKey">The instruction shown when the session resumes at its run checkpoint.</param>
internal sealed record PracticalTourStepDefinition(
    PracticalTourStep Step,
    string? TitleKey,
    string? InstructionKey,
    OnboardingPane? Pane = null,
    string? HighlightKey = null,
    bool IsVisible = true,
    bool RequiresTrainingScope = false,
    bool NeedsFindings = false,
    bool NeedsVisualizationChoice = false,
    string? UnavailableTitleKey = null,
    string? CleanRunInstructionKey = null,
    string? TutorialSampleInstructionKey = null,
    string? CheckpointInstructionKey = null);

/// <summary>Holds the ordered steps of the practical tour and what each step presents.</summary>
/// <remarks>
/// This table is the only place that knows the step order and the texts, pane and spotlight of a step.
/// The state machine takes its order from here, the view model takes its visible rows and instructions,
/// and the session takes the pane, spotlight and training-scope decisions. Adding or moving a step is one
/// row plus its texts instead of edits in several switches.
/// </remarks>
internal static class PracticalTourStepCatalog
{
    /// <summary>The base name of the resource set that holds the step texts.</summary>
    internal const string ResourceBaseName =
        "Revit.Linter.Localization.PracticalTourPresenter.ViewModels.PracticalTourViewModel";

    /// <summary>Gets the steps in tour order, including the final completed step.</summary>
    internal static readonly PracticalTourStepDefinition[] Definitions =
    [
        new(PracticalTourStep.OpenDocument, "openDocumentStep_text", "openDocument_text",
            OnboardingPane.Diagnostics,
            TutorialSampleInstructionKey: "openTutorialSample_text"),
        new(PracticalTourStep.OpenConfigurationFolder, "openConfigurationFolderStep_text", "openConfigurationFolder_text",
            OnboardingPane.Diagnostics, "OpenConfiguration"),
        new(PracticalTourStep.SearchAndFilters, "searchAndFiltersStep_text", "searchAndFilters_text",
            OnboardingPane.Diagnostics, "SearchAndFilters"),
        new(PracticalTourStep.SelectDiagnostic, "selectDiagnosticStep_text", "selectDiagnostic_text",
            OnboardingPane.Diagnostics, "SelectDiagnostic", RequiresTrainingScope: true),
        new(PracticalTourStep.RunDiagnostics, "runDiagnosticsStep_text", "runDiagnostics_text",
            OnboardingPane.Diagnostics, "RunDiagnostics", RequiresTrainingScope: true,
            CheckpointInstructionKey: "resumeFromRun_text"),
        new(PracticalTourStep.InspectFinding, "inspectFindingStep_text", "inspectFinding_text",
            OnboardingPane.Warnings, "InspectFinding", RequiresTrainingScope: true, NeedsFindings: true),
        new(PracticalTourStep.OpenHelp, "openHelpStep_text", "openHelp_text",
            OnboardingPane.Warnings, "OpenHelp", RequiresTrainingScope: true, NeedsFindings: true),
        new(PracticalTourStep.ShowElement, "showElementStep_text", "showElement_text",
            OnboardingPane.Warnings, "ShowElement", RequiresTrainingScope: true, NeedsFindings: true),
        new(PracticalTourStep.SelectVisualization, "selectVisualizationStep_text", "selectVisualization_text",
            OnboardingPane.Warnings, "ShowElement", RequiresTrainingScope: true, NeedsFindings: true,
            NeedsVisualizationChoice: true, UnavailableTitleKey: "selectVisualizationUnavailableStep_text"),
        new(PracticalTourStep.NavigateFindings, "navigateFindingsStep_text", "navigateFindings_text",
            OnboardingPane.Warnings, "NavigateFindings", RequiresTrainingScope: true, NeedsFindings: true),
        new(PracticalTourStep.UnderstandFix, "inspectFixesStep_text", "understandFix_text",
            OnboardingPane.Warnings, "UnderstandFix", RequiresTrainingScope: true, NeedsFindings: true),
        new(PracticalTourStep.FixList, "fixListStep_text", "fixList_text",
            OnboardingPane.FixList, "FixList", RequiresTrainingScope: true),
        new(PracticalTourStep.UnderstandIgnore, "understandIgnoreStep_text", "understandIgnore_text",
            OnboardingPane.Warnings, "UnderstandFix", RequiresTrainingScope: true, NeedsFindings: true),
        new(PracticalTourStep.IgnoreList, "ignoreListStep_text", "ignoreList_text",
            OnboardingPane.IgnoreList, "IgnoreList", RequiresTrainingScope: true, NeedsFindings: true),
        new(PracticalTourStep.ExportReport, "exportReportStep_text", "exportReport_text",
            HighlightKey: "ExportReport", RequiresTrainingScope: true,
            CleanRunInstructionKey: "cleanRunExportReport_text"),
        new(PracticalTourStep.ExportReportAnotherFormat, "exportReportAnotherFormatStep_text", "exportReportAnotherFormat_text",
            HighlightKey: "ExportReport", RequiresTrainingScope: true),
        new(PracticalTourStep.Completed, null, "completed_text", IsVisible: false),
    ];

    private static readonly Dictionary<PracticalTourStep, PracticalTourStepDefinition> DefinitionsByStep =
        Definitions.ToDictionary(definition => definition.Step);

    /// <summary>Gets the steps in tour order, including the final completed step.</summary>
    internal static readonly PracticalTourStep[] Steps =
        Definitions.Select(definition => definition.Step).ToArray();

    /// <summary>Gets the definitions of the steps shown in the step list, in tour order.</summary>
    internal static readonly PracticalTourStepDefinition[] VisibleSteps =
        Definitions.Where(definition => definition.IsVisible).ToArray();

    /// <summary>Gets the definition of a step.</summary>
    /// <param name="step">The step to describe.</param>
    /// <returns>The definition registered for the step.</returns>
    /// <exception cref="System.InvalidOperationException">The step has no definition in this catalog.</exception>
    internal static PracticalTourStepDefinition Get(PracticalTourStep step) =>
        DefinitionsByStep.TryGetValue(step, out PracticalTourStepDefinition? definition)
            ? definition
            : throw new InvalidOperationException($"The practical-tour step '{step}' has no definition.");

    /// <summary>Gets a value indicating whether the catalog publishes only the training check during the step.</summary>
    /// <param name="step">The step to test.</param>
    internal static bool RequiresTrainingScope(PracticalTourStep step) => Get(step).RequiresTrainingScope;
}
