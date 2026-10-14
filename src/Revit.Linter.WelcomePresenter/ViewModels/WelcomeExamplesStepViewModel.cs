using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Revit.Linter.Localization;
using Revit.Linter.WelcomePresenter.Abstractions.Models;
using Revit.Linter.WelcomePresenter.Abstractions.Services;
using Revit.Linter.WelcomePresenter.Infrastructure.Services;
using System.IO;

namespace Revit.Linter.WelcomePresenter.ViewModels;

[GenerateLocalizedProperties]
internal sealed partial class WelcomeExamplesStepViewModel : WelcomeStepViewModel
{
    private readonly IExampleConfigurationInstaller _installer;
    private readonly ILogger<WelcomeExamplesStepViewModel> _logger;

    public WelcomeExamplesStepViewModel(
        IExampleConfigurationInstaller installer,
        ILogger<WelcomeExamplesStepViewModel> logger)
    {
        _installer = installer;
        _logger = logger;
    }

    public override string Title => TitleText;

    public override string Caption => CaptionText;

    public string TargetInfo => GetLocalizedString("target_format", _installer.TargetDirectory);

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallCommand))]
    public partial bool IsMepSelected { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallCommand))]
    public partial bool IsArchitectureSelected { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallCommand))]
    public partial bool IsStructureSelected { get; set; }

    /// <summary>Gets a value indicating whether the examples were installed during this wizard session.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallCommand))]
    public partial bool IsInstalled { get; private set; }

    /// <summary>Gets the success message shown after examples have been written.</summary>
    [ObservableProperty]
    public partial string? InstallationFeedback { get; private set; }

    /// <summary>
    /// Gets the plain details of the last installation attempt: the failure, if any.
    /// <see langword="null"/> before the first attempt and after a successful installation.
    /// </summary>
    [ObservableProperty]
    public partial string? StatusMessage { get; private set; }

    private bool CanInstall() => !IsInstalled && (IsMepSelected || IsArchitectureSelected || IsStructureSelected);

    [RelayCommand(CanExecute = nameof(CanInstall))]
    private void Install()
    {
        List<ExampleDiscipline> disciplines = [];
        if (IsMepSelected) disciplines.Add(ExampleDiscipline.Mep);
        if (IsArchitectureSelected) disciplines.Add(ExampleDiscipline.Architecture);
        if (IsStructureSelected) disciplines.Add(ExampleDiscipline.Structure);

        try
        {
            ExampleInstallationResult result = _installer.Install(disciplines);

            IsInstalled = true;
            InstallationFeedback = GetLocalizedString(
                "installed_format",
                result.Files.Count,
                Path.Combine(_installer.TargetDirectory, ExampleConfigurationInstaller.ExamplesFolderName));
            StatusMessage = null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(
                exception, "Failed to install example configurations into {Directory}", _installer.TargetDirectory);
            StatusMessage = GetLocalizedString("failed_format", exception.Message);
            InstallationFeedback = null;
        }
    }
}
