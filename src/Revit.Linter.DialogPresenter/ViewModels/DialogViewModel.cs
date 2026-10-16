using CommunityToolkit.Mvvm.ComponentModel;
using Revit.Linter.DialogPresenter.Abstractions;
using Revit.Linter.DialogPresenter.Infrastructure;
using Revit.Linter.Localization;
using System.Windows;

namespace Revit.Linter.DialogPresenter.ViewModels;

[XamlConstructor]
[GenerateLocalizedProperties]
internal sealed partial class DialogViewModel : ObservableObject, IDialog
{
    private readonly DialogWindowFactory _windowFactory;

    public DialogViewModel(DialogWindowFactory windowFactory)
    {
        _windowFactory = windowFactory;
    }

    [ObservableProperty]
    public partial object? Content { get; private set; }

    public Task Show(DialogRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Content = request.Content;

        Window window = _windowFactory.CreateDialog(this);

        using CancellationTokenRegistration registration = cancellationToken.Register(
            () => _ = window.Dispatcher.InvokeAsync(window.Close));
        window.ShowDialog();

        return Task.CompletedTask;
    }

    public Task Show(object content, CancellationToken cancellationToken = default) =>
        Show(new DialogRequest(content), cancellationToken);
}
