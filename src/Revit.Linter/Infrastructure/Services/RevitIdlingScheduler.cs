using Autodesk.Revit.UI;
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
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_application is not null) return;

        _application = application;
        _application.Idling += OnIdling;
    }

    public Task RunAsync(Action<UIApplication> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        ObjectDisposedException.ThrowIf(_disposed, this);

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

    private sealed class ScheduledAction
    {
        private readonly Action<UIApplication> _action;
        private readonly CancellationToken _cancellationToken;
        private readonly TaskCompletionSource<object?> _completionSource =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ScheduledAction(Action<UIApplication> action, CancellationToken cancellationToken)
        {
            _action = action;
            _cancellationToken = cancellationToken;
        }

        public Task Task => _completionSource.Task;

        public void Execute(UIApplication uiApplication)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _completionSource.TrySetCanceled(_cancellationToken);
                return;
            }

            try
            {
                _action(uiApplication);
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
