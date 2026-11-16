using System.Reflection;
using Microsoft.Extensions.Logging;
using Revit.Linter.Core.Abstractions;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Updater;
using Revit.Linter.Updater.Core.Models;
using Revit.Linter.Updater.Core.Services;
using Serilog;

const string instanceGateName = ProductIdentity.UpdaterMutexName;

bool revitStartupCheck = args.Contains("--check-for-revit", StringComparer.OrdinalIgnoreCase);
bool skipVersionRequested = args.Contains("--skip-version", StringComparer.OrdinalIgnoreCase);
if (revitStartupCheck && skipVersionRequested)
    return 2;

string? revitResultPath = revitStartupCheck ? GetOptionValue(args, "--check-for-revit") : null;
string? versionToSkip = skipVersionRequested ? GetOptionValue(args, "--skip-version") : null;
if (revitStartupCheck && string.IsNullOrWhiteSpace(revitResultPath) ||
    skipVersionRequested && string.IsNullOrWhiteSpace(versionToSkip))
{
    return 2;
}

bool manual = !revitStartupCheck && !skipVersionRequested &&
    args.Contains("--check-now", StringComparer.OrdinalIgnoreCase);

using var instanceGate = new UpdaterInstanceGate(instanceGateName);
if (!instanceGate.TryAcquire())
{
    if (manual)
        instanceGate.RequestManualCheck();
    return 3;
}

try
{
    string updaterDirectory = UpdaterPaths.GetUpdaterDirectory();
    Directory.CreateDirectory(updaterDirectory);
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .WriteTo.File(
            UpdaterPaths.GetLogPath(updaterDirectory),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14)
        .CreateLogger();

    using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.AddSerilog(dispose: false);
    });

    string mode;
    if (revitStartupCheck)
        mode = "Revit startup";
    else if (skipVersionRequested)
        mode = "skip version";
    else if (manual)
        mode = "manual";
    else
        mode = "automatic";
    Log.Information("Updater started in {CheckMode} mode", mode);

    var stateStore = new JsonUpdaterStateStore();
    if (skipVersionRequested)
    {
        var skipService = new UpdateSkipService(
            stateStore,
            loggerFactory.CreateLogger<UpdateSkipService>());
        return await skipService.SkipAsync(versionToSkip) ? 0 : 2;
    }

    Assembly assembly = Assembly.GetExecutingAssembly();
    string? productVersion = assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
    Version? assemblyVersion = assembly.GetName().Version;
    if (assemblyVersion is null || assemblyVersion.Build < 0)
    {
        Log.Warning("Cannot determine installed version from {ProductVersion}", productVersion);
        return 2;
    }

    var currentVersion = new StableVersion(
        assemblyVersion.Major,
        assemblyVersion.Minor,
        assemblyVersion.Build);

    Log.Information("Installed version is {CurrentVersion} ({ProductVersion})", currentVersion, productVersion);

    UpdaterState userState;
    try
    {
        userState = await stateStore.LoadAsync();
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
    {
        // UpdateCoordinator owns reporting the same state failure; configuration falls back silently here.
        _ = exception;
        userState = new UpdaterState();
    }
    var policySource = new WindowsRegistryUpdaterPolicySource(
        loggerFactory.CreateLogger<WindowsRegistryUpdaterPolicySource>());
    UpdaterConfiguration configuration = new UpdaterConfigurationResolver(policySource).Resolve(userState);

    using var releaseHandler = new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        ConnectTimeout = TimeSpan.FromSeconds(10)
    };
    using var httpClient = new HttpClient(releaseHandler) { Timeout = TimeSpan.FromSeconds(10) };
    var coordinator = new UpdateCoordinator(
        new GitHubReleaseClient(
            httpClient,
            configuration.ReleaseApiUri,
            loggerFactory.CreateLogger<GitHubReleaseClient>()),
        stateStore,
        configuration,
        TimeProvider.System,
        loggerFactory.CreateLogger<UpdateCoordinator>());

    if (revitStartupCheck)
    {
        UpdateCheckResult startupResult = await coordinator.CheckAsync(
            currentVersion,
            UpdateCheckMode.Startup);
        if (startupResult.Status == UpdateCheckStatus.UpdateAvailable && startupResult.Release is not null)
        {
            try
            {
                await RevitUpdateNotificationStore.WriteAsync(
                    revitResultPath!,
                    new RevitUpdateNotification(
                        startupResult.Release.Version.ToString(),
                        startupResult.Release.ReleasePage.AbsoluteUri));
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
            {
                Log.Error(exception, "Failed to write the Revit startup update result");
                return 1;
            }
        }

        Log.Information("Revit startup update check finished with {Status}", startupResult.Status);
        if (!instanceGate.ConsumeManualCheckRequest())
            return startupResult.Status == UpdateCheckStatus.Failed ? 1 : 0;

        // A manual check was requested while this startup check held the gate; run it here so the
        // user's click is not lost, then fall through to the normal notification path.
        Log.Information("Running a manual check requested during the startup check");
        manual = true;
    }

    using var downloadHandler = new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        ConnectTimeout = TimeSpan.FromSeconds(15)
    };
    using var downloadHttpClient = new HttpClient(downloadHandler) { Timeout = Timeout.InfiniteTimeSpan };
    var activationHandler = new UpdateNotificationActivationHandler(
        stateStore,
        loggerFactory.CreateLogger<UpdateNotificationActivationHandler>());
    var installerDownloader = new InstallerDownloadService(
        downloadHttpClient,
        loggerFactory.CreateLogger<InstallerDownloadService>());
    using var notificationService = new WindowsUpdateNotificationService(
        activationHandler,
        installerDownloader,
        loggerFactory.CreateLogger<WindowsUpdateNotificationService>());
    var notificationCoordinator = new UpdateNotificationCoordinator(
        notificationService,
        stateStore,
        configuration,
        loggerFactory.CreateLogger<UpdateNotificationCoordinator>());

    UpdateCheckResult result = await coordinator.CheckAsync(
        currentVersion,
        manual ? UpdateCheckMode.Manual : UpdateCheckMode.Automatic);

    bool manualCheckRequested = instanceGate.ConsumeManualCheckRequest();
    if (!manual && manualCheckRequested &&
        result.Status is UpdateCheckStatus.Disabled or UpdateCheckStatus.NotDue)
    {
        Log.Information("Running a manual check requested by another updater invocation");
        result = await coordinator.CheckAsync(currentVersion, UpdateCheckMode.Manual);
        manual = true;
    }

    bool notificationShown = await notificationCoordinator.NotifyIfNeededAsync(result);

    if (manual && !notificationShown)
        await notificationService.TryShowManualResultAsync(result);

    return result.Status == UpdateCheckStatus.Failed ? 1 : 0;
}
finally
{
    Log.Information("Updater stopped");
    await Log.CloseAndFlushAsync();
}

static string? GetOptionValue(string[] arguments, string option)
{
    for (int index = 0; index < arguments.Length - 1; index++)
    {
        if (string.Equals(arguments[index], option, StringComparison.OrdinalIgnoreCase))
            return arguments[index + 1];
    }

    return null;
}
