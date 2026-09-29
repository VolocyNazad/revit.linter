using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Revit.Linter.RunDiagnosticPresenter.ViewModels.Base;

/// <summary>
/// Provides one-time asynchronous initialization and deinitialization commands for observable view models.
/// </summary>
public abstract partial class InitializableObservableObject : ObservableObject
{
    private bool _initialized;

    #region [Initialize] Command - Initialize

    /// <summary> Initialize </summary>
    [RelayCommand(CanExecute = nameof(CanInitialize))]
    private async Task Initialize(CancellationToken cancellationToken = default)
    {
        await OnInitializing(cancellationToken);
        _initialized = true;
    }

    private bool CanInitialize() => !_initialized;

    #endregion

    #region [Deinitialize] Command - Deinitialize

    /// <summary> Deinitialize </summary>
    [RelayCommand(CanExecute = nameof(CanDeinitialize))]
    private async Task Deinitialize(CancellationToken cancellationToken = default)
    {
        await OnDeinitializing(cancellationToken);
        _initialized = false;
    }

    private bool CanDeinitialize() => _initialized;

    #endregion

    /// <summary>
    /// Performs derived initialization before the view model is marked as initialized.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel initialization.</param>
    /// <returns>A task representing initialization.</returns>
    protected virtual Task OnInitializing(CancellationToken cancellationToken = default) { return Task.CompletedTask; }

    /// <summary>
    /// Performs derived cleanup before the view model is marked as deinitialized.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel deinitialization.</param>
    /// <returns>A task representing deinitialization.</returns>
    protected virtual Task OnDeinitializing(CancellationToken cancellationToken = default) { return Task.CompletedTask; }
}
