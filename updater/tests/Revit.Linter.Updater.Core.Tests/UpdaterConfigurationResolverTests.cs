using Revit.Linter.Updater.Core.Abstractions;
using Revit.Linter.Updater.Core.Models;
using Revit.Linter.Updater.Core.Services;

namespace Revit.Linter.Updater.Core.Tests;

public sealed class UpdaterConfigurationResolverTests
{
    [Fact]
    public void Resolve_NoPolicies_UsesUserSettingsAndDefaults()
    {
        var resolver = new UpdaterConfigurationResolver(new FakePolicySource());

        UpdaterConfiguration configuration = resolver.Resolve(new UpdaterState
        {
            AutomaticChecksEnabled = false,
            NotificationsEnabled = false
        });

        Assert.True(configuration.ChecksEnabled);
        Assert.False(configuration.AutomaticChecksEnabled);
        Assert.False(configuration.NotificationsEnabled);
        Assert.Equal(UpdaterConfiguration.DefaultAutomaticCheckInterval, configuration.AutomaticCheckInterval);
        Assert.Equal(UpdaterConfiguration.DefaultReleaseApiUri, configuration.ReleaseApiUri);
    }

    [Fact]
    public void Resolve_UserPolicyOverridesUserSettings()
    {
        var policies = new FakePolicySource
        {
            User = new UpdaterPolicy
            {
                NotificationsEnabled = true,
                AutomaticCheckInterval = TimeSpan.FromHours(12)
            }
        };
        var resolver = new UpdaterConfigurationResolver(policies);

        UpdaterConfiguration configuration = resolver.Resolve(new UpdaterState
        {
            NotificationsEnabled = false
        });

        Assert.True(configuration.NotificationsEnabled);
        Assert.Equal(TimeSpan.FromHours(12), configuration.AutomaticCheckInterval);
    }

    [Fact]
    public void Resolve_MachinePolicyOverridesUserPolicy()
    {
        var machineEndpoint = new Uri("https://machine.example.test/releases/latest");
        var policies = new FakePolicySource
        {
            User = new UpdaterPolicy
            {
                ChecksEnabled = true,
                NotificationsEnabled = true,
                ReleaseApiUri = new Uri("https://user.example.test/releases/latest")
            },
            Machine = new UpdaterPolicy
            {
                ChecksEnabled = false,
                NotificationsEnabled = false,
                ReleaseApiUri = machineEndpoint
            }
        };
        var resolver = new UpdaterConfigurationResolver(policies);

        UpdaterConfiguration configuration = resolver.Resolve(new UpdaterState());

        Assert.False(configuration.ChecksEnabled);
        Assert.False(configuration.NotificationsEnabled);
        Assert.Equal(machineEndpoint, configuration.ReleaseApiUri);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(721)]
    public void Resolve_InvalidPolicyInterval_UsesDefault(int hours)
    {
        var policies = new FakePolicySource
        {
            Machine = new UpdaterPolicy { AutomaticCheckInterval = TimeSpan.FromHours(hours) }
        };

        UpdaterConfiguration configuration =
            new UpdaterConfigurationResolver(policies).Resolve(new UpdaterState());

        Assert.Equal(UpdaterConfiguration.DefaultAutomaticCheckInterval, configuration.AutomaticCheckInterval);
    }

    [Fact]
    public void Resolve_NonHttpsPolicyEndpoint_UsesDefault()
    {
        var policies = new FakePolicySource
        {
            Machine = new UpdaterPolicy { ReleaseApiUri = new Uri("http://example.test/releases/latest") }
        };

        UpdaterConfiguration configuration =
            new UpdaterConfigurationResolver(policies).Resolve(new UpdaterState());

        Assert.Equal(UpdaterConfiguration.DefaultReleaseApiUri, configuration.ReleaseApiUri);
    }

    private sealed class FakePolicySource : IUpdaterPolicySource
    {
        public UpdaterPolicy Machine { get; init; } = new();
        public UpdaterPolicy User { get; init; } = new();

        public UpdaterPolicy ReadMachinePolicy() => Machine;

        public UpdaterPolicy ReadUserPolicy() => User;
    }
}
