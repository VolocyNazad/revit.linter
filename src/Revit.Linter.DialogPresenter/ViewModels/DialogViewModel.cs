using CommunityToolkit.Mvvm.ComponentModel;
using Revit.Context.Abstractions.Services;
using Revit.Linter.DialogPresenter.Abstractions;
using Revit.Linter.DialogPresenter.Views;
using System.Windows;
using System.Windows.Interop;

namespace Revit.Linter.DialogPresenter.ViewModels;

[XamlConstructor]
internal sealed partial class DialogViewModel : ObservableObject, IDialog
{
    private readonly IRevitContext _revitContext;

    public DialogViewModel(IRevitContext revitContext)
    {
        _revitContext = revitContext;
    }

    [ObservableProperty]
    public partial object? Content { get; private set; }

    public Task Show(object content, CancellationToken cancellationToken = default)
    {
        Content = content;

        Window window = new DialogView() {
            DataContext = this,
        };

        WindowInteropHelper helper = new(window) {
            Owner = _revitContext.UIApplication!.MainWindowHandle
        };

        window.ShowDialog();

        return Task.CompletedTask;
    }
}
