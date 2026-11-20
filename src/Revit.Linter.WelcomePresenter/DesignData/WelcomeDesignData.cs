#pragma warning disable S2325 // The XAML designer binds to instance members only, so design properties stay non-static.

using CommunityToolkit.Mvvm.Input;
using Revit.Linter.Localization;
using Revit.Linter.WelcomePresenter.Abstractions.Models;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows.Input;

namespace Revit.Linter.WelcomePresenter.DesignData;

/// <summary>
/// Provides representative sample values for <see cref="Views.WelcomeView"/> in the XAML designer.
/// </summary>
/// <remarks>The preview shows the main page, matching the design-time content template of the view.</remarks>
internal sealed class WelcomeDesignData
{
    private const string ResourceBaseName = "Revit.Linter.Localization.WelcomePresenter.ViewModels.WelcomeViewModel";

    /// <summary>Initializes the sample wizard state with the main page selected.</summary>
    public WelcomeDesignData()
    {
        WelcomePracticalStepDesignData practical = new();
        WelcomeExamplesStepDesignData examples = new();
        WelcomeIntroStepDesignData intro = new();
        AddStepRows(intro, practical, examples);
        intro.Contents.Add(new WelcomeContentsEntryDesignData
        {
            Title = intro.QuickStartText,
            Summary = intro.QuickStartSummaryText,
            ActionGlyph = "\u2197",
        });
        intro.Contents.Add(new WelcomeContentsEntryDesignData
        {
            Title = intro.DocumentationText,
            Summary = intro.DocumentationSummaryText,
            ActionGlyph = "\u2197",
        });
        intro.Contents.Add(new WelcomeContentsEntryDesignData
        {
            Title = intro.SupportText,
            Summary = intro.SupportSummaryText,
            ActionGlyph = "\u2197",
        });
        CurrentStep = intro;
    }

    private static void AddStepRows(WelcomeIntroStepDesignData intro, params WelcomeStepDesignData[] steps)
    {
        foreach (WelcomeStepDesignData step in steps)
        {
            intro.Contents.Add(new WelcomeContentsEntryDesignData
            {
                Title = step.Title,
                Summary = step.Summary,
                CommandParameter = step,
                ActionGlyph = "\u2192",
            });
        }
    }

    /// <summary>Gets the sample window title.</summary>
    public string WindowTitle => LocalizationResourceReader.GetString(ResourceBaseName, "window_title");

    /// <summary>Gets the sample version of the installed product.</summary>
    public string VersionText => string.Format(
        CultureInfo.CurrentCulture,
        LocalizationResourceReader.GetString(ResourceBaseName, "version_format"),
        "3.0.0");

    /// <summary>Gets the localized close button text.</summary>
    public string CloseButtonText => LocalizationResourceReader.GetString(ResourceBaseName, "closeButton_text");

    /// <summary>Gets the localized back button text.</summary>
    public string BackButtonText => LocalizationResourceReader.GetString(ResourceBaseName, "backButton_text");

    /// <summary>Gets a value indicating whether the back arrow is shown; the main page has nowhere to go back.</summary>
    public bool CanGoBack => false;

    /// <summary>Gets the dialog result requested by the finish command; no preset result is shown.</summary>
    public bool? DialogResult => null;

    /// <summary>Gets the command that moves to the previous step.</summary>
    public ICommand GoBackCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the command that moves to the clicked step.</summary>
    public ICommand GoToStepCommand { get; } = new RelayCommand<WelcomeStepDesignData?>(_ => { });

    /// <summary>Gets the command that closes the wizard with the requested result.</summary>
    public ICommand CloseWithResultCommand { get; } = new RelayCommand<bool>(_ => { });

    /// <summary>Gets the command of the window footer action; it does nothing in the designer.</summary>
    public ICommand PrimaryActionCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the sample step rendered in the content area.</summary>
    public WelcomeIntroStepDesignData CurrentStep { get; }
}

/// <summary>Provides the state shared by the sample wizard steps.</summary>
internal abstract class WelcomeStepDesignData
{
    /// <summary>Gets the short step name shown in the step list.</summary>
    public abstract string Title { get; }

    /// <summary>Gets the caption shown above the step content.</summary>
    public abstract string Caption { get; }

    /// <summary>Gets a value indicating whether the step shows a caption above its content.</summary>
    public bool HasCaption => Caption.Length > 0;

    /// <summary>Gets the one-line description shown in the contents of the welcome window.</summary>
    public abstract string Summary { get; }

    /// <summary>Gets a value indicating whether the window footer shows the action of the step; the main page has none.</summary>
    public virtual bool HasPrimaryAction => false;

    /// <summary>Gets the label of the window footer action; it is empty when the step has none.</summary>
    public virtual string PrimaryActionText => string.Empty;

    /// <summary>Gets the tooltip of the window footer action; it is empty when the step has none.</summary>
    public virtual string? PrimaryActionToolTip => null;
}

/// <summary>Describes one sample row of the main-page contents.</summary>
internal sealed class WelcomeContentsEntryDesignData
{
    /// <summary>Gets the row title.</summary>
    public required string Title { get; init; }

    /// <summary>Gets the one-line description shown under the title.</summary>
    public required string Summary { get; init; }

    /// <summary>Gets the command the row runs; it does nothing in the designer.</summary>
    public ICommand Command { get; } = new RelayCommand(() => { });

    /// <summary>Gets the command parameter of a step row.</summary>
    public object? CommandParameter { get; init; }

    /// <summary>Gets the glyph that shows where the row leads.</summary>
    public required string ActionGlyph { get; init; }
}

/// <summary>Provides sample values for the intro step content of <see cref="Views.WelcomeView"/>.</summary>
internal sealed class WelcomeIntroStepDesignData : WelcomeStepDesignData
{
    private const string ResourceBaseName = "Revit.Linter.Localization.WelcomePresenter.ViewModels.WelcomeIntroStepViewModel";

    /// <inheritdoc />
    public override string Title => LocalizationResourceReader.GetString(ResourceBaseName, "title_text");

    /// <inheritdoc />
    public override string Caption => string.Empty;

    /// <inheritdoc />
    public override string Summary => LocalizationResourceReader.GetString(ResourceBaseName, "summary_text");

    /// <summary>Gets the sample rows listed in the contents of the main page.</summary>
    public ObservableCollection<WelcomeContentsEntryDesignData> Contents { get; } = [];

    /// <summary>Gets the sample introduction sentence.</summary>
    public string AboutText => LocalizationResourceReader.GetString(ResourceBaseName, "about_text");

    /// <summary>Gets the localized quick-start text.</summary>
    public string QuickStartText => LocalizationResourceReader.GetString(ResourceBaseName, "quickStart_text");

    /// <summary>Gets the localized quick-start description.</summary>
    public string QuickStartSummaryText =>
        LocalizationResourceReader.GetString(ResourceBaseName, "quickStartSummary_text");

    /// <summary>Gets the localized documentation text.</summary>
    public string DocumentationText => LocalizationResourceReader.GetString(ResourceBaseName, "documentation_text");

    /// <summary>Gets the localized documentation description.</summary>
    public string DocumentationSummaryText =>
        LocalizationResourceReader.GetString(ResourceBaseName, "documentationSummary_text");

    /// <summary>Gets the localized support text.</summary>
    public string SupportText => LocalizationResourceReader.GetString(ResourceBaseName, "support_text");

    /// <summary>Gets the localized support description.</summary>
    public string SupportSummaryText =>
        LocalizationResourceReader.GetString(ResourceBaseName, "supportSummary_text");
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

    /// <inheritdoc />
    public override string Summary => LocalizationResourceReader.GetString(ResourceBaseName, "summary_text");

    /// <inheritdoc />
    public override bool HasPrimaryAction => true;

    /// <inheritdoc />
    public override string PrimaryActionText => InstallButtonText;

    /// <inheritdoc />
    public override string? PrimaryActionToolTip => TargetInfo;

    /// <summary>Gets the sample header of the regular examples column.</summary>
    public string ExamplesHeaderText =>
        LocalizationResourceReader.GetString(ResourceBaseName, "examplesHeader_text");

    /// <summary>Gets the sample label of the MEP discipline.</summary>
    public string MepText => LocalizationResourceReader.GetString(ResourceBaseName, "mep_text");

    /// <summary>Gets the sample label of the architecture discipline.</summary>
    public string ArchitectureText => LocalizationResourceReader.GetString(ResourceBaseName, "architecture_text");

    /// <summary>Gets the sample label of the structure discipline.</summary>
    public string StructureText => LocalizationResourceReader.GetString(ResourceBaseName, "structure_text");

    /// <summary>Gets the sample description of the installed content.</summary>
    public string ContentsText => LocalizationResourceReader.GetString(ResourceBaseName, "contents_text");

    /// <summary>Gets the sample header of the ADSK template check block.</summary>
    public string AdskChecksHeaderText => LocalizationResourceReader.GetString(ResourceBaseName, "adskChecksHeader_text");

    /// <summary>Gets the sample introduction of the ADSK template check block.</summary>
    public string AdskChecksIntroText => LocalizationResourceReader.GetString(ResourceBaseName, "adskChecksIntro_text");

    /// <summary>Gets the sample label of the architecture check pack.</summary>
    public string AdskCheckArchitectureText =>
        LocalizationResourceReader.GetString(ResourceBaseName, "adskCheckArchitecture_text");

    /// <summary>Gets the sample label of the water supply check pack.</summary>
    public string AdskCheckWaterSupplyText =>
        LocalizationResourceReader.GetString(ResourceBaseName, "adskCheckWaterSupply_text");

    /// <summary>Gets the sample label of the reinforced concrete check pack.</summary>
    public string AdskCheckReinforcedConcreteText =>
        LocalizationResourceReader.GetString(ResourceBaseName, "adskCheckReinforcedConcrete_text");

    /// <summary>Gets the sample label of the steel structures check pack.</summary>
    public string AdskCheckSteelStructuresText =>
        LocalizationResourceReader.GetString(ResourceBaseName, "adskCheckSteelStructures_text");

    /// <summary>Gets the sample label of the heating and ventilation check pack.</summary>
    public string AdskCheckHvacText => LocalizationResourceReader.GetString(ResourceBaseName, "adskCheckHvac_text");

    /// <summary>Gets the sample label of the low-current systems check pack.</summary>
    public string AdskCheckLowCurrentText =>
        LocalizationResourceReader.GetString(ResourceBaseName, "adskCheckLowCurrent_text");

    /// <summary>Gets the sample label of the technology check pack.</summary>
    public string AdskCheckTechnologyText =>
        LocalizationResourceReader.GetString(ResourceBaseName, "adskCheckTechnology_text");

    /// <summary>Gets the sample label of the electrical check pack.</summary>
    public string AdskCheckElectricalText =>
        LocalizationResourceReader.GetString(ResourceBaseName, "adskCheckElectrical_text");

    /// <summary>Gets or sets a value indicating whether the architecture check pack is selected.</summary>
    public bool IsCheckArchitectureSelected { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the water supply check pack is selected.</summary>
    public bool IsCheckWaterSupplySelected { get; set; }

    /// <summary>Gets or sets a value indicating whether the reinforced concrete check pack is selected.</summary>
    public bool IsCheckReinforcedConcreteSelected { get; set; }

    /// <summary>Gets or sets a value indicating whether the steel structures check pack is selected.</summary>
    public bool IsCheckSteelStructuresSelected { get; set; }

    /// <summary>Gets or sets a value indicating whether the heating and ventilation check pack is selected.</summary>
    public bool IsCheckHvacSelected { get; set; }

    /// <summary>Gets or sets a value indicating whether the low-current systems check pack is selected.</summary>
    public bool IsCheckLowCurrentSelected { get; set; }

    /// <summary>Gets or sets a value indicating whether the technology check pack is selected.</summary>
    public bool IsCheckTechnologySelected { get; set; }

    /// <summary>Gets or sets a value indicating whether the electrical check pack is selected.</summary>
    public bool IsCheckElectricalSelected { get; set; }

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

    /// <inheritdoc />
    public override string Summary => LocalizationResourceReader.GetString(ResourceBaseName, "summary_text");

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

    /// <summary>Gets the sample note of the Autodesk sample selection; no note is shown in the designer.</summary>
    public string? TutorialSampleStatusText => null;

    /// <summary>Gets the sample action that follows completion of the wizard.</summary>
    public string NextActionText => LocalizationResourceReader.GetString(ResourceBaseName, "prepareSample_text");

    /// <summary>Gets the sample remark about the practical tour.</summary>
    public string Note => LocalizationResourceReader.GetString(ResourceBaseName, "practicalTourReset_text");

    /// <summary>Gets the localized reset button text.</summary>
    public string ResetPracticalTourText => LocalizationResourceReader.GetString(ResourceBaseName, "resetPracticalTour_text");

    /// <summary>Gets the command that resets the practical tour; it does nothing in the designer.</summary>
    public ICommand ResetPracticalTourCommand { get; } = new RelayCommand(() => { });
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
