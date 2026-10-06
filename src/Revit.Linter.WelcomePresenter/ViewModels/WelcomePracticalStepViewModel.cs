using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Revit.Linter.Localization;
using Revit.Linter.WelcomePresenter.Abstractions;
using Revit.Linter.WelcomePresenter.Abstractions.Models;
using Revit.Linter.WelcomePresenter.Abstractions.Services;
using System.Collections.ObjectModel;
using System.IO;

namespace Revit.Linter.WelcomePresenter.ViewModels;

/// <summary>Configures the optional practical tour and its Autodesk sample.</summary>
[GenerateLocalizedProperties]
internal sealed partial class WelcomePracticalStepViewModel : WelcomeStepViewModel
{
    private readonly IWelcomeHost _host;
    private readonly ITutorialSampleCatalog _sampleCatalog;
    private readonly ITutorialSampleCopyService _sampleCopyService;
    private readonly ILogger<WelcomePracticalStepViewModel> _logger;
    private readonly IPracticalTour _practicalTour;

    public WelcomePracticalStepViewModel(
        IWelcomeHost host,
        ITutorialSampleCatalog sampleCatalog,
        ITutorialSampleCopyService sampleCopyService,
        IPracticalTour practicalTour,
        ILogger<WelcomePracticalStepViewModel> logger)
    {
        _host = host;
        _sampleCatalog = sampleCatalog;
        _sampleCopyService = sampleCopyService;
        _practicalTour = practicalTour;
        _logger = logger;
        StartPracticalTour = true;
    }

    public override string Title => TitleText;

    public override string Caption => CaptionText;

    /// <summary>Gets the action that follows completion of the welcome wizard.</summary>
    public string NextActionText
    {
        get
        {
            if (!StartPracticalTour) return SkipTourText;
            if (_host.HasOpenDocument) return StartWithOpenDocumentText;
            return UseTutorialSample && SelectedTutorialSample is not null
                ? PrepareSampleText
                : WaitForDocumentText;
        }
    }

    public ObservableCollection<TutorialSampleCandidate> TutorialSamples { get; } = [];

    public bool CanUseTutorialSample => !_host.HasOpenDocument && TutorialSamples.Count > 0;

    public bool CanSelectTutorialSample => StartPracticalTour && UseTutorialSample;

    public string TutorialSampleStatusText => _host.HasOpenDocument
        ? OpenDocumentHintText
        : CanUseTutorialSample ? TutorialSampleHintText : TutorialSampleUnavailableText;

    [ObservableProperty]
    public partial TutorialSampleCandidate? SelectedTutorialSample { get; set; }

    [ObservableProperty]
    public partial bool UseTutorialSample { get; set; }

    /// <summary>Gets or sets whether the practical tour starts after the wizard closes.</summary>
    [ObservableProperty]
    public partial bool StartPracticalTour { get; set; }

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
        foreach (TutorialSampleCandidate sample in _sampleCatalog.Find(preferredDisciplines))
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
            string path = _sampleCopyService.CreateCopy(SelectedTutorialSample, revitVersion);
            _host.QueueTutorialSample(path);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(exception, "Failed to prepare tutorial sample {SamplePath}", SelectedTutorialSample.SourcePath);
            Note = TutorialSampleFailedText;
            return false;
        }
    }

    [RelayCommand]
    private void ResetPracticalTour()
    {
        _practicalTour.ResetProgress();
        StartPracticalTour = true;
        Note = PracticalTourResetText;
    }
}
