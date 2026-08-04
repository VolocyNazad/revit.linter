using Revit.Linter.ValueStore.Abstractions.Models;
using Revit.Linter.ValueStore.Services;

namespace Revit.Linter.ValueStore.Tests;

public sealed class ValueStoreNotificationHubTests
{
    [Fact]
    public void Identical_failure_is_reported_once()
    {
        ValueStoreNotificationHub hub = new();
        int notifications = 0;
        using IDisposable subscription = hub.OnLoadFailed(_ => notifications++);

        hub.ReportFailure(typeof(TestSettings), "settings.yml", new InvalidOperationException("Invalid YAML"));
        hub.ReportFailure(typeof(TestSettings), "settings.yml", new InvalidOperationException("Invalid YAML"));

        Assert.Equal(1, notifications);
    }

    [Fact]
    public void Current_failure_is_replayed_to_late_subscriber()
    {
        ValueStoreNotificationHub hub = new();
        hub.ReportFailure(typeof(TestSettings), "settings.yml", new InvalidOperationException("Invalid YAML"));
        ValueStoreLoadFailedEventArgs? received = null;

        using IDisposable subscription = hub.OnLoadFailed(args => received = args);

        Assert.NotNull(received);
        Assert.Equal(typeof(TestSettings), received.SettingsType);
        Assert.Equal("settings.yml", received.FilePath);
    }

    [Fact]
    public void Successful_load_allows_same_failure_to_be_reported_again()
    {
        ValueStoreNotificationHub hub = new();
        int notifications = 0;
        using IDisposable subscription = hub.OnLoadFailed(_ => notifications++);
        hub.ReportFailure(typeof(TestSettings), "settings.yml", new InvalidOperationException("Invalid YAML"));

        hub.ReportSuccess(typeof(TestSettings));
        hub.ReportFailure(typeof(TestSettings), "settings.yml", new InvalidOperationException("Invalid YAML"));

        Assert.Equal(2, notifications);
    }

    [Fact]
    public void Disposed_subscription_is_not_notified()
    {
        ValueStoreNotificationHub hub = new();
        int notifications = 0;
        IDisposable subscription = hub.OnLoadFailed(_ => notifications++);
        subscription.Dispose();
        subscription.Dispose();

        hub.ReportFailure(typeof(TestSettings), "settings.yml", new InvalidOperationException("Invalid YAML"));

        Assert.Equal(0, notifications);
    }
}
