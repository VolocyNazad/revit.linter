#pragma warning disable S2325 // The XAML designer binds to instance members only, so design properties stay non-static.

using CommunityToolkit.Mvvm.Input;
using Revit.Linter.Localization;
using System.Windows.Input;

namespace Revit.Linter.DialogPresenter.DesignData;

/// <summary>Provides representative sample values for <see cref="Views.UpdateAvailableDialogView"/> in the XAML designer.</summary>
internal sealed class UpdateAvailableDialogDesignData
{
    private const string ResourceBaseName = "Revit.Linter.Localization.DialogPresenter.ViewModels.UpdateAvailableDialogViewModel";

    /// <summary>Gets the sample notification text, rendered through the string template of the view.</summary>
    public object Content =>
        "A new version of Revit Linter is available: 3.2.0. Open the GitHub release page to download and install it.";

    /// <summary>Gets the localized close button text.</summary>
    public string CloseButtonText => LocalizationResourceReader.GetString(ResourceBaseName, "closeButton_text");

    /// <summary>Gets the localized skip button text.</summary>
    public string SkipButtonText => LocalizationResourceReader.GetString(ResourceBaseName, "skipButton_text");

    /// <summary>Gets the localized update button text.</summary>
    public string UpdateButtonText => LocalizationResourceReader.GetString(ResourceBaseName, "updateButton_text");

    /// <summary>Gets the dialog result the window behavior applies; no preset result is shown.</summary>
    public bool? DialogResult => null;

    /// <summary>Gets the command that closes the dialog without remembering the release.</summary>
    public ICommand CloseCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the command that closes the dialog and skips the release.</summary>
    public ICommand SkipVersionCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the command that closes the dialog and opens the release page.</summary>
    public ICommand OpenReleaseCommand { get; } = new RelayCommand(() => { });
}
