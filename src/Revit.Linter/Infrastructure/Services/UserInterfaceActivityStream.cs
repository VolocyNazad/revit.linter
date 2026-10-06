using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace Revit.Linter.Infrastructure.Services;

internal sealed class UserInterfaceActivityStream(ILogger<UserInterfaceActivityStream> logger)
    : IUserInterfaceActivityStream
{
    public event EventHandler<UserInterfaceActivity>? ActivityPublished;

    public void Publish(UserInterfaceActivity activity)
    {
        Delegate[] subscribers = ActivityPublished?.GetInvocationList() ?? [];
        foreach (Delegate subscriber in subscribers)
        {
            try
            {
                ((EventHandler<UserInterfaceActivity>)subscriber).Invoke(this, activity);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Optional UI activity observer failed while handling {ActivityType}",
                    activity.GetType().Name);
            }
        }
    }
}
