
using Revit.Async.Interfaces;


namespace Revit.Async.Entities
{
    internal class DefaultResultHandler<TResult> :
        IExternalEventResultHandler<TResult>
    {

        public DefaultResultHandler(TaskCompletionSource<TResult> taskCompletionSource)
        {
            TaskCompletionSource = taskCompletionSource;
        }



        private TaskCompletionSource<TResult> TaskCompletionSource { get; }



        public void Cancel()
        {
            TaskCompletionSource.TrySetCanceled();
        }

        public void SetResult(TResult result)
        {
            TaskCompletionSource.TrySetResult(result);
        }

        public void ThrowException(Exception exception)
        {
            TaskCompletionSource.TrySetException(exception);
        }

    }
}
