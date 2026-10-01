using System.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Revit.Linter.Updater.Core.Abstractions;
using Revit.Linter.Updater.Core.Models;

namespace Revit.Linter.Updater;

internal sealed class WindowsRegistryUpdaterPolicySource : IUpdaterPolicySource
{
    internal const string PolicySubKey = @"Software\Policies\Volocy\Revit.Linter\Updater";
    private readonly ILogger<WindowsRegistryUpdaterPolicySource> _logger;

    public WindowsRegistryUpdaterPolicySource(ILogger<WindowsRegistryUpdaterPolicySource> logger) =>
        _logger = logger;

    public UpdaterPolicy ReadMachinePolicy() => ReadPolicy(RegistryHive.LocalMachine);

    public UpdaterPolicy ReadUserPolicy() => ReadPolicy(RegistryHive.CurrentUser);

    private UpdaterPolicy ReadPolicy(RegistryHive hive)
    {
        try
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using RegistryKey? policyKey = baseKey.OpenSubKey(PolicySubKey, writable: false);
            if (policyKey is null)
                return new UpdaterPolicy();

            return new UpdaterPolicy
            {
                ChecksEnabled = ReadBoolean(policyKey, "ChecksEnabled"),
                NotificationsEnabled = ReadBoolean(policyKey, "NotificationsEnabled"),
                AutomaticCheckInterval = ReadInterval(policyKey, "CheckIntervalHours"),
                ReleaseApiUri = ReadHttpsUri(policyKey, "ReleaseApiUrl")
            };
        }
        catch (Exception exception) when (
            exception is SecurityException or UnauthorizedAccessException or IOException)
        {
            _logger.LogWarning(exception, "Failed to read updater policy from {RegistryHive}", hive);
            return new UpdaterPolicy();
        }
    }

    private bool? ReadBoolean(RegistryKey key, string valueName)
    {
        object? value = key.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
        if (value is null)
            return null;
        if (value is int integer && integer is 0 or 1)
            return integer == 1;

        _logger.LogWarning("Ignored invalid updater policy value {PolicyValue}", valueName);
        return null;
    }

    private TimeSpan? ReadInterval(RegistryKey key, string valueName)
    {
        object? value = key.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
        if (value is null)
            return null;
        if (value is int hours and >= 1 and <= 720)
            return TimeSpan.FromHours(hours);

        _logger.LogWarning("Ignored invalid updater policy value {PolicyValue}", valueName);
        return null;
    }

    private Uri? ReadHttpsUri(RegistryKey key, string valueName)
    {
        object? value = key.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
        if (value is null)
            return null;
        if (value is string text &&
            Uri.TryCreate(text, UriKind.Absolute, out Uri? uri) &&
            uri.Scheme == Uri.UriSchemeHttps)
        {
            return uri;
        }

        _logger.LogWarning("Ignored invalid updater policy value {PolicyValue}", valueName);
        return null;
    }
}
