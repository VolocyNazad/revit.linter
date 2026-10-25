#pragma warning disable S2325 // The XAML designer binds to instance members only, so design properties stay non-static.

using CommunityToolkit.Mvvm.Input;
using Revit.Linter.Localization;
using Revit.Linter.WelcomePresenter.Abstractions.Models;
using System.Globalization;
using System.IO;
using System.Windows.Input;

namespace Revit.Linter.WelcomePresenter.DesignData;

/// <summary>
/// Provides representative sample values for <see cref="Views.WelcomeView"/> in the XAML designer.
/// </summary>
/// <remarks>The preview shows the rules step, matching the design-time content template of the view.</remarks>
internal sealed class WelcomeDesignData
{
    private const string ResourceBaseName = "Revit.Linter.Localization.WelcomePresenter.ViewModels.WelcomeViewModel";

    /// <summary>Initializes the sample wizard state with the rules step selected.</summary>
    public WelcomeDesignData()
    {
        Steps =
        [
            new WelcomeIntroStepDesignData { Number = 1 },
            CurrentStep,
            new WelcomeExamplesStepDesignData { Number = 3 },
            new WelcomePracticalStepDesignData { Number = 4 },
            new WelcomeFinishStepDesignData { Number = 5 },
        ];
    }

    /// <summary>Gets the sample window title.</summary>
    public string WindowTitle => LocalizationResourceReader.GetString(ResourceBaseName, "window_title");

    /// <summary>Gets the sample progress caption of the selected step.</summary>
    public string Progress => string.Format(
        CultureInfo.CurrentCulture,
        LocalizationResourceReader.GetString(ResourceBaseName, "step_format"),
        2,
        5);

    /// <summary>Gets the localized close button text.</summary>
    public string CloseButtonText => LocalizationResourceReader.GetString(ResourceBaseName, "closeButton_text");

    /// <summary>Gets the localized back button text.</summary>
    public string BackButtonText => LocalizationResourceReader.GetString(ResourceBaseName, "backButton_text");

    /// <summary>Gets the localized next button text.</summary>
    public string NextButtonText => LocalizationResourceReader.GetString(ResourceBaseName, "nextButton_text");

    /// <summary>Gets the localized finish button text.</summary>
    public string FinishButtonText => LocalizationResourceReader.GetString(ResourceBaseName, "finishButton_text");

    /// <summary>Gets a value indicating whether a following step exists; the rules step is not the last one.</summary>
    public bool HasNextStep => true;

    /// <summary>Gets a value indicating whether the selected step ends the wizard; the rules step does not.</summary>
    public bool IsLastStep => false;

    /// <summary>Gets the dialog result requested by the finish command; no preset result is shown.</summary>
    public bool? DialogResult => null;

    /// <summary>Gets the command that moves to the previous step.</summary>
    public ICommand GoBackCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the command that moves to the next step.</summary>
    public ICommand GoNextCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the command that moves to the clicked step.</summary>
    public ICommand GoToStepCommand { get; } = new RelayCommand<WelcomeStepDesignData?>(_ => { });

    /// <summary>Gets the command that closes the wizard with the requested result.</summary>
    public ICommand CloseWithResultCommand { get; } = new RelayCommand<bool>(_ => { });

    /// <summary>Gets the sample steps shown in the step strip.</summary>
    public IReadOnlyList<WelcomeStepDesignData> Steps { get; }

    /// <summary>Gets the sample step rendered in the content area.</summary>
    public WelcomeRulesStepDesignData CurrentStep { get; } = new() { Number = 2, IsCurrent = true };
}

/// <summary>Provides the state shared by the sample wizard steps.</summary>
internal abstract class WelcomeStepDesignData
{
    /// <summary>Gets the one-based position of the step among the shown steps.</summary>
    public int Number { get; init; }

    /// <summary>Gets a value indicating whether the wizard currently displays this step.</summary>
    public bool IsCurrent { get; init; }

    /// <summary>Gets the short step name shown in the step strip.</summary>
    public abstract string Title { get; }

    /// <summary>Gets the caption shown above the step content.</summary>
    public abstract string Caption { get; }
}

/// <summary>Provides sample values for the intro step content of <see cref="Views.WelcomeView"/>.</summary>
internal sealed class WelcomeIntroStepDesignData : WelcomeStepDesignData
{
    private const string ResourceBaseName = "Revit.Linter.Localization.WelcomePresenter.ViewModels.WelcomeIntroStepViewModel";

    /// <inheritdoc />
    public override string Title => LocalizationResourceReader.GetString(ResourceBaseName, "title_text");

    /// <inheritdoc />
    public override string Caption => LocalizationResourceReader.GetString(ResourceBaseName, "caption_text");

    /// <summary>Gets the sample introduction sentence.</summary>
    public string AboutText => LocalizationResourceReader.GetString(ResourceBaseName, "about_text");

    /// <summary>Gets the sample workflow pipeline.</summary>
    public string WorkflowText => LocalizationResourceReader.GetString(ResourceBaseName, "workflow_text");

    /// <summary>Gets the sample header of the important information block.</summary>
    public string ImportantHeaderText => LocalizationResourceReader.GetString(ResourceBaseName, "importantHeader_text");

    /// <summary>Gets the sample sentence naming the ribbon tab.</summary>
    public string TabInfo => string.Format(
        CultureInfo.CurrentCulture,
        LocalizationResourceReader.GetString(ResourceBaseName, "tab_format"),
        "Revit Linter");
}

/// <summary>Provides sample values for the rules step content of <see cref="Views.WelcomeView"/>.</summary>
internal sealed class WelcomeRulesStepDesignData : WelcomeStepDesignData
{
    private const string ResourceBaseName = "Revit.Linter.Localization.WelcomePresenter.ViewModels.WelcomeRulesStepViewModel";

    /// <inheritdoc />
    public override string Title => LocalizationResourceReader.GetString(ResourceBaseName, "title_text");

    /// <inheritdoc />
    public override string Caption => LocalizationResourceReader.GetString(ResourceBaseName, "caption_text");

    /// <summary>Gets the sample title of the built-in diagnostics column.</summary>
    public string BuiltInTitleText => LocalizationResourceReader.GetString(ResourceBaseName, "builtInTitle_text");

    /// <summary>Gets the sample description of the built-in diagnostics column.</summary>
    public string BuiltInDescriptionText => LocalizationResourceReader.GetString(ResourceBaseName, "builtInDescription_text");

    /// <summary>Gets the sample title of the user-defined rules column.</summary>
    public string UserTitleText => LocalizationResourceReader.GetString(ResourceBaseName, "userTitle_text");

    /// <summary>Gets the sample description of the user-defined rules column.</summary>
    public string UserDescriptionText => LocalizationResourceReader.GetString(ResourceBaseName, "userDescription_text");

    /// <summary>Gets the sample title of the rule-kind list.</summary>
    public string RuleKindsTitleText => LocalizationResourceReader.GetString(ResourceBaseName, "ruleKindsTitle_text");

    /// <summary>Gets the sample description of the element rule kind.</summary>
    public string ElementRuleKindText => LocalizationResourceReader.GetString(ResourceBaseName, "elementRuleKind_text");

    /// <summary>Gets the sample description of the collision rule kind.</summary>
    public string CollisionRuleKindText => LocalizationResourceReader.GetString(ResourceBaseName, "collisionRuleKind_text");

    /// <summary>Gets the sample description of the project parameter rule kind.</summary>
    public string ParameterRuleKindText => LocalizationResourceReader.GetString(ResourceBaseName, "parameterRuleKind_text");

    /// <summary>Gets the sample title of the YAML examples section.</summary>
    public string VisualizerTitleText => LocalizationResourceReader.GetString(ResourceBaseName, "visualizerTitle_text");

    /// <summary>Gets the sample title of the element rule preview.</summary>
    public string ElementPreviewTitleText => LocalizationResourceReader.GetString(ResourceBaseName, "elementPreviewTitle_text");

    /// <summary>Gets the sample element rule in YAML.</summary>
    public string RulePreviewText => LocalizationResourceReader.GetString(ResourceBaseName, "rulePreview_text");

    /// <summary>Gets the sample explanation of the code key.</summary>
    public string CodeExplanationText => LocalizationResourceReader.GetString(ResourceBaseName, "codeExplanation_text");

    /// <summary>Gets the sample explanation of the description key.</summary>
    public string DescriptionExplanationText => LocalizationResourceReader.GetString(ResourceBaseName, "descriptionExplanation_text");

    /// <summary>Gets the sample explanation of the severity key.</summary>
    public string SeverityExplanationText => LocalizationResourceReader.GetString(ResourceBaseName, "severityExplanation_text");

    /// <summary>Gets the sample explanation of the take key.</summary>
    public string TakeExplanationText => LocalizationResourceReader.GetString(ResourceBaseName, "takeExplanation_text");

    /// <summary>Gets the sample explanation of the check key.</summary>
    public string CheckExplanationText => LocalizationResourceReader.GetString(ResourceBaseName, "checkExplanation_text");

    /// <summary>Gets the sample title of the collision rule preview.</summary>
    public string CollisionPreviewTitleText => LocalizationResourceReader.GetString(ResourceBaseName, "collisionPreviewTitle_text");

    /// <summary>Gets the sample collision rule in YAML.</summary>
    public string CollisionRulePreviewText => LocalizationResourceReader.GetString(ResourceBaseName, "collisionRulePreview_text");

    /// <summary>Gets the sample explanation of the andTake key.</summary>
    public string AndTakeExplanationText => LocalizationResourceReader.GetString(ResourceBaseName, "andTakeExplanation_text");

    /// <summary>Gets the sample explanation of the groupBy key.</summary>
    public string GroupByExplanationText => LocalizationResourceReader.GetString(ResourceBaseName, "groupByExplanation_text");

    /// <summary>Gets the sample explanation of the intersection placeholder.</summary>
    public string IntersectionExplanationText => LocalizationResourceReader.GetString(ResourceBaseName, "intersectionExplanation_text");

    /// <summary>Gets the sample explanation of the collision take key.</summary>
    public string CollisionTakeExplanationText => LocalizationResourceReader.GetString(ResourceBaseName, "collisionTakeExplanation_text");

    /// <summary>Gets the sample title of the project parameter rule preview.</summary>
    public string ProjectParameterPreviewTitleText => LocalizationResourceReader.GetString(ResourceBaseName, "projectParameterPreviewTitle_text");

    /// <summary>Gets the sample project parameter rule in YAML, with the current Revit parameter group.</summary>
    public string ProjectParameterRulePreviewText => LocalizationResourceReader
        .GetString(ResourceBaseName, "projectParameterRuleTemplate_text")
        .Replace("{parameterGroup}", "autodesk.parameter.group:data-1.0.0");

    /// <summary>Gets the sample explanation of the parameters key.</summary>
    public string ParametersExplanationText => LocalizationResourceReader.GetString(ResourceBaseName, "parametersExplanation_text");

    /// <summary>Gets the sample explanation of the guid key.</summary>
    public string GuidExplanationText => LocalizationResourceReader.GetString(ResourceBaseName, "guidExplanation_text");

    /// <summary>Gets the sample explanation of the group key.</summary>
    public string GroupExplanationText => LocalizationResourceReader.GetString(ResourceBaseName, "groupExplanation_text");

    /// <summary>Gets the sample explanation of the categories key.</summary>
    public string CategoriesExplanationText => LocalizationResourceReader.GetString(ResourceBaseName, "categoriesExplanation_text");

    /// <summary>Gets the sample explanation of the isInstance key.</summary>
    public string IsInstanceExplanationText => LocalizationResourceReader.GetString(ResourceBaseName, "isInstanceExplanation_text");

    /// <summary>Gets the sample explanation of the allowVaryBetweenGroups key.</summary>
    public string AllowVaryExplanationText => LocalizationResourceReader.GetString(ResourceBaseName, "allowVaryExplanation_text");
}

/// <summary>Provides sample values for the examples step content of <see cref="Views.WelcomeView"/>.</summary>
internal sealed class WelcomeExamplesStepDesignData : WelcomeStepDesignData
{
    private const string ResourceBaseName = "Revit.Linter.Localization.WelcomePresenter.ViewModels.WelcomeExamplesStepViewModel";

    /// <summary>Gets the sample target directory of the example configuration.</summary>
    private static readonly string SampleTargetDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "Autodesk",
        "Revit",
        "Linter",
        "Configuration");

    /// <inheritdoc />
    public override string Title => LocalizationResourceReader.GetString(ResourceBaseName, "title_text");

    /// <inheritdoc />
    public override string Caption => LocalizationResourceReader.GetString(ResourceBaseName, "caption_text");

    /// <summary>Gets the sample introduction sentence.</summary>
    public string IntroText => LocalizationResourceReader.GetString(ResourceBaseName, "intro_text");

    /// <summary>Gets the sample label of the MEP discipline.</summary>
    public string MepText => LocalizationResourceReader.GetString(ResourceBaseName, "mep_text");

    /// <summary>Gets the sample label of the architecture discipline.</summary>
    public string ArchitectureText => LocalizationResourceReader.GetString(ResourceBaseName, "architecture_text");

    /// <summary>Gets the sample label of the structure discipline.</summary>
    public string StructureText => LocalizationResourceReader.GetString(ResourceBaseName, "structure_text");

    /// <summary>Gets the sample description of the installed content.</summary>
    public string ContentsText => LocalizationResourceReader.GetString(ResourceBaseName, "contents_text");

    /// <summary>Gets the sample sentence naming the installation target.</summary>
    public string TargetInfo => string.Format(
        CultureInfo.CurrentCulture,
        LocalizationResourceReader.GetString(ResourceBaseName, "target_format"),
        SampleTargetDirectory);

    /// <summary>Gets or sets a value indicating whether the MEP discipline is selected.</summary>
    public bool IsMepSelected { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the architecture discipline is selected.</summary>
    public bool IsArchitectureSelected { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the structure discipline is selected.</summary>
    public bool IsStructureSelected { get; set; }

    /// <summary>Gets a value indicating whether the examples were installed during this wizard session.</summary>
    public bool IsInstalled => true;

    /// <summary>Gets the sample success message shown after the examples have been written.</summary>
    public string InstallationFeedback => string.Format(
        CultureInfo.CurrentCulture,
        LocalizationResourceReader.GetString(ResourceBaseName, "installed_format"),
        6,
        SampleTargetDirectory);

    /// <summary>Gets the plain failure details of the last installation attempt; no failure is shown.</summary>
    public string? StatusMessage => null;

    /// <summary>Gets the command that installs the selected examples; it does nothing in the designer.</summary>
    public ICommand InstallCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the localized install button text.</summary>
    public string InstallButtonText => LocalizationResourceReader.GetString(ResourceBaseName, "installButton_text");
}

/// <summary>Provides sample values for the practical-tour step content of <see cref="Views.WelcomeView"/>.</summary>
internal sealed class WelcomePracticalStepDesignData : WelcomeStepDesignData
{
    private const string ResourceBaseName = "Revit.Linter.Localization.WelcomePresenter.ViewModels.WelcomePracticalStepViewModel";

    /// <summary>Gets the sample Autodesk samples available for the practical tour.</summary>
    public IReadOnlyList<TutorialSampleCandidate> TutorialSamples { get; } =
    [
        new(
            Path.Combine(SampleDirectory, "rac_basic_sample_project.rvt"),
            "rac_basic_sample_project.rvt",
            ExampleDiscipline.Architecture),
        new(
            Path.Combine(SampleDirectory, "rme_advanced_sample_project.rvt"),
            "rme_advanced_sample_project.rvt",
            ExampleDiscipline.Mep),
    ];

    /// <summary>Gets the sample folder that stores the Autodesk samples.</summary>
    private static string SampleDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "Autodesk",
        "Revit",
        "Samples");

    /// <summary>Initializes the sample step with the first available Autodesk sample selected.</summary>
    public WelcomePracticalStepDesignData() => SelectedTutorialSample = TutorialSamples[0];

    /// <inheritdoc />
    public override string Title => LocalizationResourceReader.GetString(ResourceBaseName, "title_text");

    /// <inheritdoc />
    public override string Caption => LocalizationResourceReader.GetString(ResourceBaseName, "caption_text");

    /// <summary>Gets the localized practical-tour checkbox text.</summary>
    public string StartPracticalTourText => LocalizationResourceReader.GetString(ResourceBaseName, "startPracticalTour_text");

    /// <summary>Gets or sets a value indicating whether the practical tour starts after the wizard closes.</summary>
    public bool StartPracticalTour { get; set; } = true;

    /// <summary>Gets the localized text of the Autodesk sample checkbox.</summary>
    public string UseTutorialSampleText => LocalizationResourceReader.GetString(ResourceBaseName, "useTutorialSample_text");

    /// <summary>Gets or sets a value indicating whether an Autodesk sample is used for the tour.</summary>
    public bool UseTutorialSample { get; set; } = true;

    /// <summary>Gets a value indicating whether an Autodesk sample can be used for the tour.</summary>
    public bool CanUseTutorialSample => true;

    /// <summary>Gets a value indicating whether the sample selection can be changed.</summary>
    public bool CanSelectTutorialSample => true;

    /// <summary>Gets or sets the selected Autodesk sample; it is an item of <see cref="TutorialSamples"/>.</summary>
    public TutorialSampleCandidate? SelectedTutorialSample { get; set; }

    /// <summary>Gets the sample status of the Autodesk sample selection.</summary>
    public string TutorialSampleStatusText => LocalizationResourceReader.GetString(ResourceBaseName, "tutorialSampleHint_text");

    /// <summary>Gets the sample action that follows completion of the wizard.</summary>
    public string NextActionText => LocalizationResourceReader.GetString(ResourceBaseName, "prepareSample_text");

    /// <summary>Gets the sample remark about the practical tour.</summary>
    public string Note => LocalizationResourceReader.GetString(ResourceBaseName, "practicalTourReset_text");

    /// <summary>Gets the localized reset button text.</summary>
    public string ResetPracticalTourText => LocalizationResourceReader.GetString(ResourceBaseName, "resetPracticalTour_text");

    /// <summary>Gets the command that resets the practical tour; it does nothing in the designer.</summary>
    public ICommand ResetPracticalTourCommand { get; } = new RelayCommand(() => { });
}

/// <summary>Provides sample values for the finish step content of <see cref="Views.WelcomeView"/>.</summary>
internal sealed class WelcomeFinishStepDesignData : WelcomeStepDesignData
{
    private const string ResourceBaseName = "Revit.Linter.Localization.WelcomePresenter.ViewModels.WelcomeFinishStepViewModel";

    /// <inheritdoc />
    public override string Title => LocalizationResourceReader.GetString(ResourceBaseName, "title_text");

    /// <inheritdoc />
    public override string Caption => LocalizationResourceReader.GetString(ResourceBaseName, "caption_text");

    /// <summary>Gets the sample sentence explaining how to reopen the wizard.</summary>
    public string ReopenInfo => string.Format(
        CultureInfo.CurrentCulture,
        LocalizationResourceReader.GetString(ResourceBaseName, "reopen_format"),
        "Revit Linter");

    /// <summary>Gets the localized quick-start button text.</summary>
    public string QuickStartText => LocalizationResourceReader.GetString(ResourceBaseName, "quickStart_text");

    /// <summary>Gets the localized documentation button text.</summary>
    public string DocumentationText => LocalizationResourceReader.GetString(ResourceBaseName, "documentation_text");

    /// <summary>Gets the localized support button text.</summary>
    public string SupportText => LocalizationResourceReader.GetString(ResourceBaseName, "support_text");

    /// <summary>Gets the command that opens the quick start; it does nothing in the designer.</summary>
    public ICommand OpenQuickStartCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the command that opens the documentation; it does nothing in the designer.</summary>
    public ICommand OpenDocumentationCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the command that opens the support page; it does nothing in the designer.</summary>
    public ICommand OpenSupportCommand { get; } = new RelayCommand(() => { });
}

/// <summary>Provides representative sample values for <see cref="Views.PracticalTourView"/> in the XAML designer.</summary>
internal sealed class PracticalTourDesignData
{
    private const string ResourceBaseName = "Revit.Linter.Localization.WelcomePresenter.ViewModels.PracticalTourViewModel";

    /// <summary>Gets the sample tour title.</summary>
    public string TitleText => LocalizationResourceReader.GetString(ResourceBaseName, "title_text");

    /// <summary>Gets the sample tour steps; the configuration-folder step is the current one.</summary>
    public IReadOnlyList<PracticalTourStepDesignData> Steps { get; } =
    [
        new(
            GetString("openDocumentStep_text"),
            GetString("openDocument_text"),
            false,
            true,
            false),
        new(
            GetString("openConfigurationFolderStep_text"),
            GetString("openConfigurationFolder_text"),
            true,
            false,
            false),
        new(
            GetString("searchAndFiltersStep_text"),
            GetString("searchAndFilters_text"),
            false,
            false,
            false),
        new(
            GetString("selectDiagnosticStep_text"),
            GetString("selectDiagnostic_text"),
            false,
            false,
            false),
        new(
            GetString("runDiagnosticsStep_text"),
            GetString("runDiagnostics_text"),
            false,
            false,
            false),
        new(
            GetString("inspectFindingStep_text"),
            GetString("inspectFinding_text"),
            false,
            false,
            false),
        new(
            GetString("openHelpStep_text"),
            GetString("openHelp_text"),
            false,
            false,
            false),
        new(
            GetString("showElementStep_text"),
            GetString("showElement_text"),
            false,
            false,
            false),
        new(
            GetString("selectVisualizationStep_text"),
            GetString("selectVisualization_text"),
            false,
            false,
            false),
        new(
            GetString("navigateFindingsStep_text"),
            GetString("navigateFindings_text"),
            false,
            false,
            false),
        new(
            GetString("inspectFixesStep_text"),
            GetString("understandFix_text"),
            false,
            false,
            false),
        new(
            GetString("fixListStep_text"),
            GetString("fixList_text"),
            false,
            false,
            false),
        new(
            GetString("understandIgnoreStep_text"),
            GetString("understandIgnore_text"),
            false,
            false,
            false),
        new(
            GetString("ignoreListStep_text"),
            GetString("ignoreList_text"),
            false,
            false,
            false),
        new(
            GetString("exportReportStep_text"),
            GetString("exportReport_text"),
            false,
            false,
            false),
        new(
            GetString("exportReportAnotherFormatStep_text"),
            GetString("exportReportAnotherFormat_text"),
            false,
            false,
            false),
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
