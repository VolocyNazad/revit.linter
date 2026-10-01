using System.Reflection;
using Microsoft.Extensions.Logging;
using Revit.Linter.Updater;
using Revit.Linter.Updater.Core.Models;
using Revit.Linter.Updater.Core.Services;
using Serilog;

const string instanceGateName = "Volocy.Revit.Linter.Updater";
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
    string updaterDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Volocy", "Revit.Linter", "updater");
    Directory.CreateDirectory(updaterDirectory);
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .WriteTo.File(
            Path.Combine(updaterDirectory, "logs", "updater-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14)
        .CreateLogger();

    string? productVersion = Assembly.GetExecutingAssembly()
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
    if (!StableVersion.TryParse(productVersion, out StableVersion currentVersion))
    {
        Log.Warning("Cannot determine installed version from {ProductVersion}", productVersion);
        if (manual)
            await Console.Error.WriteLineAsync($"Cannot determine installed version from '{productVersion}'.");
        return 2;
    }

    Log.Information(
        "Updater {ProductVersion} started in {CheckMode} mode",
        currentVersion,
        manual ? "manual" : "automatic");

    using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.AddSerilog(dispose: true);
        if (manual)
        {
            builder.AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
            });
        }
    });
    using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    using var downloadHttpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
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

    await notificationCoordinator.NotifyIfNeededAsync(result);

    if (manual)
    {
        string message = result.Status switch
        {
            UpdateCheckStatus.UpdateAvailable =>
                $"Revit Linter {result.Release!.Version} is available: {result.Release.ReleasePage}",
            UpdateCheckStatus.UpToDate => $"Revit Linter {currentVersion} is up to date.",
            UpdateCheckStatus.Skipped => $"Revit Linter {result.Release!.Version} is available but skipped.",
            UpdateCheckStatus.Failed => $"Update check failed: {result.Error}",
            _ => $"Update check finished with status {result.Status}."
        };
        await Console.Out.WriteLineAsync(message);
    }

    return result.Status == UpdateCheckStatus.Failed ? 1 : 0;
}
finally
{
    Log.Information("Updater stopped");
    await Log.CloseAndFlushAsync();
}
