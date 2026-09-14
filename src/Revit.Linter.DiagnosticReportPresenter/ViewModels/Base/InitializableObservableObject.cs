using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Revit.Linter.DiagnosticReportPresenter.ViewModels;

public abstract partial class InitializableObservableObject : ObservableObject
{
    private bool _initialized;

    #region [Initialize] Command - Initialize

    /// <summary> Initialize </summary>
    [RelayCommand(CanExecute = nameof(CanInitialize))]
    internal async Task Initialize(CancellationToken cancellationToken = default)
    {
        await OnInitializing(cancellationToken);
        _initialized = true;
    }

    private bool CanInitialize() => !_initialized;

    #endregion

    #region [Deinitialize] Command - Deinitialize

    /// <summary> Deinitialize </summary>
    [RelayCommand(CanExecute = nameof(CanDeinitialize))]
    internal async Task Deinitialize(CancellationToken cancellationToken = default)
    {
        await OnDeinitializing(cancellationToken);
        _initialized = false;
    }

    private bool CanDeinitialize() => _initialized;

    #endregion

    protected virtual Task OnInitializing(CancellationToken cancellationToken = default) { return Task.CompletedTask; }
    protected virtual Task OnDeinitializing(CancellationToken cancellationToken = default) { return Task.CompletedTask; }
}
