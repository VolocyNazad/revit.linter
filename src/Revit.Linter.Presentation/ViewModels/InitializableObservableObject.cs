using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Revit.Linter.Presentation.ViewModels;

/// <summary>
/// Provides one-time asynchronous initialization and deinitialization commands for observable view models.
/// </summary>
/// <remarks>
/// <c>InitializeCommand</c> can run only while the view model is not initialized and
/// <c>DeinitializeCommand</c> only while it is, so a view may bind both to its loaded and unloaded events.
/// </remarks>
public abstract partial class InitializableObservableObject : ObservableObject
{
    private bool _initialized;

    /// <summary>Runs the derived initialization and marks the view model as initialized.</summary>
    /// <param name="cancellationToken">A token that can cancel initialization.</param>
    /// <returns>A task representing initialization.</returns>
    [RelayCommand(CanExecute = nameof(CanInitialize))]
    public async Task Initialize(CancellationToken cancellationToken = default)
    {
        await OnInitializing(cancellationToken);
        _initialized = true;
    }

    private bool CanInitialize() => !_initialized;

    /// <summary>Runs the derived cleanup and marks the view model as not initialized.</summary>
    /// <param name="cancellationToken">A token that can cancel deinitialization.</param>
    /// <returns>A task representing deinitialization.</returns>
    [RelayCommand(CanExecute = nameof(CanDeinitialize))]
    public async Task Deinitialize(CancellationToken cancellationToken = default)
    {
        await OnDeinitializing(cancellationToken);
        _initialized = false;
    }

    private bool CanDeinitialize() => _initialized;

    /// <summary>
    /// Performs derived initialization before the view model is marked as initialized.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel initialization.</param>
    /// <returns>A task representing initialization.</returns>
    protected virtual Task OnInitializing(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>
    /// Performs derived cleanup before the view model is marked as deinitialized.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel deinitialization.</param>
    /// <returns>A task representing deinitialization.</returns>
    protected virtual Task OnDeinitializing(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
