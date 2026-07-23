

namespace Revit.Async.Utils
{
    internal static class TaskUtils
    {

        public static Task<T> FromResult<T>(T value)
        {
#if NET40
            var tcs = new TaskCompletionSource<T>();
            tcs.TrySetResult(value);
            return tcs.Task;
#else
            return Task.FromResult(value);
#endif
        }

    }
}
