using Revit.Linter.Updater.Core.Abstractions;
using Revit.Linter.Updater.Core.Models;

namespace Revit.Linter.Updater.Core.Services;

/// <summary>Resolves effective updater behavior from policy, user state, and built-in defaults.</summary>
public sealed class UpdaterConfigurationResolver
{
    private readonly IUpdaterPolicySource _policySource;

    /// <summary>Creates a resolver backed by a read-only policy source.</summary>
    public UpdaterConfigurationResolver(IUpdaterPolicySource policySource) =>
        _policySource = policySource;

    /// <summary>Resolves configuration using machine policy, user policy, user settings, then defaults.</summary>
    /// <remarks>
    /// Higher-precedence scopes replace only values they define. Invalid intervals and non-HTTPS API
    /// endpoints are ignored defensively even if a policy source returns them.
    /// </remarks>
    public UpdaterConfiguration Resolve(UpdaterState userState)
    {
        UpdaterPolicy machinePolicy = _policySource.ReadMachinePolicy();
        UpdaterPolicy userPolicy = _policySource.ReadUserPolicy();
        var configuration = new UpdaterConfiguration(
            ChecksEnabled: true,
            AutomaticChecksEnabled: userState.AutomaticChecksEnabled,
            NotificationsEnabled: userState.NotificationsEnabled,
            UpdaterConfiguration.DefaultAutomaticCheckInterval,
            UpdaterConfiguration.DefaultReleaseApiUri);

        configuration = Apply(configuration, userPolicy);
        return Apply(configuration, machinePolicy);
    }

    private static UpdaterConfiguration Apply(
        UpdaterConfiguration configuration,
        UpdaterPolicy policy)
    {
        TimeSpan interval = policy.AutomaticCheckInterval is { } candidateInterval &&
                            candidateInterval >= TimeSpan.FromHours(1) &&
                            candidateInterval <= TimeSpan.FromDays(30)
            ? candidateInterval
            : configuration.AutomaticCheckInterval;
        Uri releaseApiUri = policy.ReleaseApiUri is { Scheme: "https" } candidateUri
            ? candidateUri
            : configuration.ReleaseApiUri;

        return configuration with
        {
            ChecksEnabled = policy.ChecksEnabled ?? configuration.ChecksEnabled,
            NotificationsEnabled = policy.NotificationsEnabled ?? configuration.NotificationsEnabled,
            AutomaticCheckInterval = interval,
            ReleaseApiUri = releaseApiUri
        };
    }
}
