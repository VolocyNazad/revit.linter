using CommunityToolkit.Mvvm.ComponentModel;
using Revit.Linter.Localization;
using Revit.Context.Abstractions.Services;
using Revit.Linter.DialogPresenter.Abstractions;
using Revit.Linter.DialogPresenter.Views;
using Revit.Linter.ThemeManaging.Abstractions.Services;
using System.Windows.Interop;

namespace Revit.Linter.DialogPresenter.ViewModels;

[XamlConstructor]
[GenerateLocalizedProperties]
internal sealed partial class ConfirmationDialogViewModel : ObservableObject, IConfirmationDialog
{
    private readonly IRevitContext _revitContext;
    private readonly IThemeService _themeService;

    public ConfirmationDialogViewModel(
        IRevitContext revitContext,
        IThemeService themeService)
    {
        _revitContext = revitContext;
        _themeService = themeService;
    }

    [ObservableProperty]
    public partial object? Content { get; private set; }

    [ObservableProperty]
    public partial string? ConfirmButtonText { get; private set; }

    public Task<bool> Show(ConfirmationDialogRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Content = request.Content;
        ConfirmButtonText = request.ConfirmButtonText;

        ConfirmationDialogView window = new() {
            DataContext = this,
        };
        _themeService.Register(window);

        _ = new WindowInteropHelper(window) {
            Owner = _revitContext.UIApplication!.MainWindowHandle
        };

        using CancellationTokenRegistration registration = cancellationToken.Register(
            () => _ = window.Dispatcher.InvokeAsync(window.Close));

        bool? result = window.ShowDialog();

        Content = null;
        ConfirmButtonText = null;

        return Task.FromResult(result == true);
    }
}
