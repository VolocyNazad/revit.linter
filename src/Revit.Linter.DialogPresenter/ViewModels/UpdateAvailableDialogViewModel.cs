using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Revit.Linter.DialogPresenter.Abstractions;
using Revit.Linter.DialogPresenter.Infrastructure;
using Revit.Linter.Localization;
using System.Globalization;
using System.Windows;

namespace Revit.Linter.DialogPresenter.ViewModels;

[XamlConstructor]
[GenerateLocalizedProperties]
internal sealed partial class UpdateAvailableDialogViewModel : ObservableObject, IUpdateAvailableDialog
{
    private readonly DialogWindowFactory _windowFactory;

    public UpdateAvailableDialogViewModel(DialogWindowFactory windowFactory)
    {
        _windowFactory = windowFactory;
    }

    [ObservableProperty]
    public partial string? Content { get; private set; }

    /// <summary>
    /// Gets the dialog result requested by the close commands; the window behavior applies it.
    /// </summary>
    [ObservableProperty]
    public partial bool? DialogResult { get; private set; }

    /// <summary>Gets the action chosen by the user; it stays <see cref="UpdateAvailableDialogChoice.Close"/> otherwise.</summary>
    public UpdateAvailableDialogChoice Choice { get; private set; }

    /// <summary>Closes the dialog without remembering the release.</summary>
    [RelayCommand]
    private void Close() => CloseWith(UpdateAvailableDialogChoice.Close);

    /// <summary>Closes the dialog and asks not to offer this release again.</summary>
    [RelayCommand]
    private void SkipVersion() => CloseWith(UpdateAvailableDialogChoice.SkipVersion);

    /// <summary>Closes the dialog and opens the GitHub release page.</summary>
    [RelayCommand]
    private void OpenRelease() => CloseWith(UpdateAvailableDialogChoice.OpenRelease);

    public Task<UpdateAvailableDialogChoice> Show(
        UpdateAvailableDialogRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Content = string.Format(CultureInfo.CurrentCulture, MessageText, request.Version);
        Choice = UpdateAvailableDialogChoice.Close;
        DialogResult = null;

        Window window = _windowFactory.CreateUpdateAvailableDialog(this);

        using CancellationTokenRegistration registration = cancellationToken.Register(
            () => _ = window.Dispatcher.InvokeAsync(window.Close));

        _ = window.ShowDialog();

        Content = null;
        DialogResult = null;

        return Task.FromResult(Choice);
    }

    private void CloseWith(UpdateAvailableDialogChoice choice)
    {
        Choice = choice;
        DialogResult = true;
    }
}
