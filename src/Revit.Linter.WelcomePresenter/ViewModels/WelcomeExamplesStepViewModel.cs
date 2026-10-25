using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Revit.Linter.Localization;
using Revit.Linter.WelcomePresenter.Abstractions.Models;
using Revit.Linter.WelcomePresenter.Abstractions.Services;
using Revit.Linter.WelcomePresenter.Infrastructure.Services;
using System.IO;

namespace Revit.Linter.WelcomePresenter.ViewModels;

/// <summary>
/// Presents the optional example-configuration step and installs the example for the selected disciplines.
/// </summary>
[GenerateLocalizedProperties]
internal sealed partial class WelcomeExamplesStepViewModel(
    IExampleConfigurationInstaller installer,
    ILogger<WelcomeExamplesStepViewModel> logger) : WelcomeStepViewModel
{
    public override string Title => TitleText;

    public override string Caption => CaptionText;

    public string TargetInfo => GetLocalizedString("target_format", installer.TargetDirectory);

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
            ExampleInstallationResult result = installer.Install(disciplines);

            IsInstalled = true;
            string examplesDirectory = Path.Combine(
                installer.TargetDirectory, ExampleConfigurationInstaller.ExamplesFolderName);
            InstalledExampleFile? installed = result.Files.FirstOrDefault();
            InstallationFeedback = installed?.Outcome switch
            {
                ExampleInstallationOutcome.Unchanged =>
                    GetLocalizedString("unchanged_format", examplesDirectory),
                ExampleInstallationOutcome.Replaced when installed.BackupPath is not null =>
                    GetLocalizedString("updated_format", examplesDirectory, installed.BackupPath),
                _ => GetLocalizedString("installed_format", examplesDirectory),
            };
            StatusMessage = null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogError(
                exception, "Failed to install example configurations into {Directory}", installer.TargetDirectory);
            StatusMessage = GetLocalizedString("failed_format", exception.Message);
            InstallationFeedback = null;
        }
    }
}
