using CommunityToolkit.Mvvm.ComponentModel;
using Revit.Context.Abstractions.Services;
using Revit.Linter.DialogPresenter.Abstractions;
using Revit.Linter.DialogPresenter.Views;
using Revit.Linter.ThemeManaging.Abstractions.Services;
using System.Windows;
using System.Windows.Interop;

namespace Revit.Linter.DialogPresenter.ViewModels;

[XamlConstructor]
internal sealed partial class DialogViewModel : ObservableObject, IDialog
{
    private readonly IRevitContext _revitContext;
    private readonly IThemeService _themeService;

    public DialogViewModel(IRevitContext revitContext, IThemeService themeService)
    {
        _revitContext = revitContext;
        _themeService = themeService;
    }

    [ObservableProperty]
    public partial object? Content { get; private set; }

    public Task Show(object content, CancellationToken cancellationToken = default)
    {
        Content = content;

        Window window = new DialogView() {
            DataContext = this,
        };
        _themeService.Register(window);

        WindowInteropHelper _ = new(window) {
            Owner = _revitContext.UIApplication!.MainWindowHandle
        };

        window.ShowDialog();

        return Task.CompletedTask;
    }
}
