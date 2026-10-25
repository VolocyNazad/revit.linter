using Autodesk.Revit.UI.Events;
using Revit.Linter.Core.Abstractions.Services;
using System.Collections.Concurrent;

namespace Revit.Linter.Infrastructure.Services;

internal sealed class RevitIdlingScheduler : IRevitIdlingScheduler, IDisposable
{
    private readonly ConcurrentQueue<ScheduledAction> _actions = new();
    private UIControlledApplication? _application;
    private bool _disposed;

    public void Initialize(UIControlledApplication application)
    {
        ThrowIfDisposed();
        if (_application is not null) return;

        _application = application;
        _application.Idling += OnIdling;
    }

    public Task RunAsync(Action<UIApplication> action, CancellationToken cancellationToken = default)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));
        ThrowIfDisposed();

        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        var scheduledAction = new ScheduledAction(action, cancellationToken);
        _actions.Enqueue(scheduledAction);
        return scheduledAction.Task;
    }

    private void OnIdling(object? sender, IdlingEventArgs e)
    {
        if (sender is not UIApplication uiApplication) return;

        while (_actions.TryDequeue(out ScheduledAction? action))
            action.Execute(uiApplication);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_application is not null)
        {
            _application.Idling -= OnIdling;
            _application = null;
        }

        while (_actions.TryDequeue(out ScheduledAction? action))
            action.Cancel();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RevitIdlingScheduler));
    }

    private sealed class ScheduledAction(
        Action<UIApplication> action, CancellationToken cancellationToken)
    {
        private readonly TaskCompletionSource<object?> _completionSource =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Task => _completionSource.Task;

        public void Execute(UIApplication uiApplication)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _completionSource.TrySetCanceled(cancellationToken);
                return;
            }

            try
            {
                action(uiApplication);
                _completionSource.TrySetResult(null);
            }
            catch (Exception exception)
            {
                _completionSource.TrySetException(exception);
            }
        }

        public void Cancel() => _completionSource.TrySetCanceled(CancellationToken.None);
    }
}
