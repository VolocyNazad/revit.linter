using CommunityToolkit.Mvvm.ComponentModel;
using Revit.Linter.Localization;
using Revit.Context.Abstractions.Services;
using Revit.Linter.DialogPresenter.Abstractions;
using Revit.Linter.DialogPresenter.Views;
using Revit.Linter.ThemeManaging.Abstractions.Services;
using System.Windows;
using System.Windows.Interop;

namespace Revit.Linter.DialogPresenter.ViewModels;

[XamlConstructor]
[GenerateLocalizedProperties]
internal sealed partial class DialogViewModel : ObservableObject, IDialog
{
    private readonly IRevitContext _revitContext;
    private readonly IThemeService _themeService;

    public DialogViewModel(
        IRevitContext revitContext,
        IThemeService themeService)
    {
        _revitContext = revitContext;
        _themeService = themeService;
    }

    [ObservableProperty]
    public partial object? Content { get; private set; }

    public Task Show(DialogRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Content = request.Content;

        Window window = new DialogView() {
            DataContext = this,
        };
        _themeService.Register(window);

        _ = new WindowInteropHelper(window) {
            Owner = _revitContext.UIApplication!.MainWindowHandle
        };

        using CancellationTokenRegistration registration = cancellationToken.Register(
            () => _ = window.Dispatcher.InvokeAsync(window.Close));
        window.ShowDialog();

        return Task.CompletedTask;
    }

    public Task Show(object content, CancellationToken cancellationToken = default) =>
        Show(new DialogRequest(content), cancellationToken);
}
