using System.Reflection;
using Microsoft.Extensions.Logging;
using Revit.Linter.Updater.Core.Models;
using Revit.Linter.Updater.Core.Services;
using Serilog;

const string instanceGateName = "Local\\Volocy.Revit.Linter.Updater";
bool manual = args.Contains("--check-now", StringComparer.OrdinalIgnoreCase);

using var instanceGate = new Semaphore(1, 1, instanceGateName);
if (!instanceGate.WaitOne(TimeSpan.Zero, false))
    return 0;

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
        if (manual)
            await Console.Error.WriteLineAsync($"Cannot determine installed version from '{productVersion}'.");
        return 2;
    }

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
    var coordinator = new UpdateCoordinator(
        new GitHubReleaseClient(httpClient, loggerFactory.CreateLogger<GitHubReleaseClient>()),
        new JsonUpdaterStateStore(),
        TimeProvider.System,
        loggerFactory.CreateLogger<UpdateCoordinator>());

    UpdateCheckResult result = await coordinator.CheckAsync(
        currentVersion,
        manual ? UpdateCheckMode.Manual : UpdateCheckMode.Automatic);

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
    await Log.CloseAndFlushAsync();
    instanceGate.Release();
}
