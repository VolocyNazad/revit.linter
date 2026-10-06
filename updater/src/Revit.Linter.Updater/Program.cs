using System.Reflection;
using Microsoft.Extensions.Logging;
using Revit.Linter.Core.Abstractions;
using Revit.Linter.Updater;
using Revit.Linter.Updater.Core.Models;
using Revit.Linter.Updater.Core.Services;
using Serilog;

const string instanceGateName = ProductIdentity.UpdaterMutexName;
bool manual = args.Contains("--check-now", StringComparer.OrdinalIgnoreCase);

using var instanceGate = new UpdaterInstanceGate(instanceGateName);
if (!instanceGate.TryAcquire())
{
    if (manual)
        instanceGate.RequestManualCheck();
    return 0;
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

    Log.Information(
        "Updater {ProductVersion} started in {CheckMode} mode",
        currentVersion,
        manual ? "manual" : "automatic");

    using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.AddSerilog(dispose: false);
    });
    using var releaseHandler = new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        ConnectTimeout = TimeSpan.FromSeconds(10)
    };
    using var downloadHandler = new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        ConnectTimeout = TimeSpan.FromSeconds(15)
    };
    using var httpClient = new HttpClient(releaseHandler) { Timeout = TimeSpan.FromSeconds(10) };
    using var downloadHttpClient = new HttpClient(downloadHandler) { Timeout = Timeout.InfiniteTimeSpan };
    var stateStore = new JsonUpdaterStateStore();
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
    var coordinator = new UpdateCoordinator(
        new GitHubReleaseClient(
            httpClient,
            configuration.ReleaseApiUri,
            loggerFactory.CreateLogger<GitHubReleaseClient>()),
        stateStore,
        configuration,
        TimeProvider.System,
        loggerFactory.CreateLogger<UpdateCoordinator>());
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
