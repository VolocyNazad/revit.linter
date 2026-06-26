using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Revit.Linter.DiagnosticReportPresenter.ViewModels;

public abstract partial class InitializableObservableObject : ObservableObject
{
    private bool _initialized;

    #region [Initialize] Command - Инициализировать 

    /// <summary> Инициализировать </summary>
    [RelayCommand(CanExecute = nameof(CanInitialize))]
    private async Task Initialize(CancellationToken cancellationToken = default)
    {
        await OnInitializing(cancellationToken);
        _initialized = true;
    }

    private bool CanInitialize() => !_initialized;

    #endregion

    #region [Deinitialize] Command - Деинициализировать 

    /// <summary> Деинициализировать </summary>
    [RelayCommand(CanExecute = nameof(CanDeinitialize))]
    private async Task Deinitialize(CancellationToken cancellationToken = default)
    {
        await OnDeinitializing(cancellationToken);
        _initialized = false;
    }

    private bool CanDeinitialize() => _initialized;

    #endregion

    protected virtual Task OnInitializing(CancellationToken cancellationToken) { return Task.CompletedTask; }
    protected virtual Task OnDeinitializing(CancellationToken cancellationToken) { return Task.CompletedTask; }
}
