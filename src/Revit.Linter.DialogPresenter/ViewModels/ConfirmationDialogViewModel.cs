using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Revit.Linter.DialogPresenter.Abstractions;
using Revit.Linter.DialogPresenter.Infrastructure;
using Revit.Linter.Localization;
using System.Windows;

namespace Revit.Linter.DialogPresenter.ViewModels;

[XamlConstructor]
[GenerateLocalizedProperties]
internal sealed partial class ConfirmationDialogViewModel : ObservableObject, IConfirmationDialog
{
    private readonly DialogWindowFactory _windowFactory;

    public ConfirmationDialogViewModel(DialogWindowFactory windowFactory)
    {
        _windowFactory = windowFactory;
    }

    [ObservableProperty]
    public partial object? Content { get; private set; }

    [ObservableProperty]
    public partial string? ConfirmButtonText { get; private set; }

    /// <summary>
    /// Gets the dialog result requested by the close commands; the window behavior applies it.
    /// </summary>
    [ObservableProperty]
    public partial bool? DialogResult { get; private set; }

    /// <summary>Closes the dialog with the given result.</summary>
    [RelayCommand]
    private void CloseWithResult(bool result) => DialogResult = result;

    public Task<bool> Show(ConfirmationDialogRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Content = request.Content;
        ConfirmButtonText = request.ConfirmButtonText;
        DialogResult = null;

        Window window = _windowFactory.CreateConfirmationDialog(this);

        using CancellationTokenRegistration registration = cancellationToken.Register(
            () => _ = window.Dispatcher.InvokeAsync(window.Close));

        bool? result = window.ShowDialog();

        Content = null;
        ConfirmButtonText = null;
        DialogResult = null;

        return Task.FromResult(result == true);
    }
}
