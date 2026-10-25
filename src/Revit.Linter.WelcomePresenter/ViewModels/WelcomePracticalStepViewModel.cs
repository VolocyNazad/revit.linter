using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Revit.Linter.Localization;
using Revit.Linter.PracticalTourPresenter.Interactions.Abstractions;
using Revit.Linter.WelcomePresenter.Abstractions;
using Revit.Linter.WelcomePresenter.Abstractions.Models;
using Revit.Linter.WelcomePresenter.Abstractions.Services;
using System.Collections.ObjectModel;
using System.IO;

namespace Revit.Linter.WelcomePresenter.ViewModels;

/// <summary>Configures the optional practical tour and its Autodesk sample.</summary>
[GenerateLocalizedProperties]
internal sealed partial class WelcomePracticalStepViewModel(
    IWelcomeHost host,
    ITutorialSampleCatalog sampleCatalog,
    ITutorialSampleCopyService sampleCopyService,
    IPracticalTour practicalTour,
    ILogger<WelcomePracticalStepViewModel> logger) : WelcomeStepViewModel
{
    public override string Title => TitleText;

    public override string Caption => CaptionText;

    /// <summary>Gets the action that follows completion of the welcome wizard.</summary>
    public string NextActionText
    {
        get
        {
            if (!StartPracticalTour) return SkipTourText;
            if (host.HasOpenDocument) return StartWithOpenDocumentText;
            return UseTutorialSample && SelectedTutorialSample is not null
                ? PrepareSampleText
                : WaitForDocumentText;
        }
    }

    public ObservableCollection<TutorialSampleCandidate> TutorialSamples { get; } = [];

    public bool CanUseTutorialSample => !host.HasOpenDocument && TutorialSamples.Count > 0;

    public bool CanSelectTutorialSample => StartPracticalTour && UseTutorialSample;

    public string TutorialSampleStatusText
    {
        get
        {
            if (host.HasOpenDocument)
                return OpenDocumentHintText;
            return CanUseTutorialSample ? TutorialSampleHintText : TutorialSampleUnavailableText;
        }
    }

    [ObservableProperty]
    public partial TutorialSampleCandidate? SelectedTutorialSample { get; set; }

    [ObservableProperty]
    public partial bool UseTutorialSample { get; set; }

    /// <summary>Gets or sets whether the practical tour starts after the wizard closes.</summary>
    [ObservableProperty]
    public partial bool StartPracticalTour { get; set; } = true;

    /// <summary>Gets or sets an optional remark about the example-installation step.</summary>
    [ObservableProperty]
    public partial string? Note { get; set; }

    partial void OnStartPracticalTourChanged(bool value)
    {
        UseTutorialSample = value && CanUseTutorialSample;
        OnPropertyChanged(nameof(CanSelectTutorialSample));
        OnPropertyChanged(nameof(NextActionText));
    }

    partial void OnUseTutorialSampleChanged(bool value)
    {
        OnPropertyChanged(nameof(CanSelectTutorialSample));
        OnPropertyChanged(nameof(NextActionText));
    }

    partial void OnSelectedTutorialSampleChanged(TutorialSampleCandidate? value)
        => OnPropertyChanged(nameof(NextActionText));

    internal void RefreshTutorialSamples(IReadOnlyCollection<ExampleDiscipline> preferredDisciplines)
    {
        TutorialSamples.Clear();
        foreach (TutorialSampleCandidate sample in sampleCatalog.Find(preferredDisciplines))
            TutorialSamples.Add(sample);
        SelectedTutorialSample = TutorialSamples.FirstOrDefault();
        UseTutorialSample = StartPracticalTour && CanUseTutorialSample;
        OnPropertyChanged(nameof(CanUseTutorialSample));
        OnPropertyChanged(nameof(CanSelectTutorialSample));
        OnPropertyChanged(nameof(TutorialSampleStatusText));
        OnPropertyChanged(nameof(NextActionText));
    }

    internal bool PrepareTutorialSample(int revitVersion)
    {
        if (!UseTutorialSample || SelectedTutorialSample is null) return false;

        try
        {
            string path = sampleCopyService.CreateCopy(SelectedTutorialSample, revitVersion);
            host.QueueTutorialSample(path);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogError(exception, "Failed to prepare tutorial sample {SamplePath}", SelectedTutorialSample.SourcePath);
            Note = TutorialSampleFailedText;
            return false;
        }
    }

    [RelayCommand]
    private void ResetPracticalTour()
    {
        try
        {
            // Reset restarts the tour from scratch through the same path as the in-pane
            // Start over command: the saved progress is forgotten, the managed configuration
            // is reinstalled and the training check selection is cleared. Resetting only the
            // saved progress would leave a live session running with no visible effect.
            practicalTour.Start(host.HasOpenDocument, restart: true);
            StartPracticalTour = true;
            Note = PracticalTourResetText;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to reset the practical tour");
            Note = PracticalTourResetFailedText;
        }
    }
}
