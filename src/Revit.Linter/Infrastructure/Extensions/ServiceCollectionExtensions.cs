using Microsoft.Extensions.DependencyInjection;
using Revit.Context.Abstractions.Services;
using Revit.Linter.SerilogEnrichers;
using Serilog;
using Serilog.Events;
using System.Diagnostics;
using System.IO;

namespace Revit.Linter.Infrastructure.Extensions;

internal static class ServiceCollectionExtensions
{
    private const long LogFileSizeLimitBytes = 20 * 1024 * 1024;
    private const int RetainedLogFileCountLimit = 14;

    /// <summary>Gets the per-user directory that receives the add-in log files.</summary>
    internal static string LogDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Volocy",
        "Revit.Linter",
        "logs");

    extension(IServiceCollection services)
    {
        public IServiceCollection AddAndConfigureSerilog()
        {
            string logDirectory = LogDirectory;
            Directory.CreateDirectory(logDirectory);
            string logPath = Path.Combine(logDirectory, "revit-linter-.log");

            var loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.WithProperty("isDebug", Debugger.IsAttached)
                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentName()
                .Enrich.WithRevitContext(() => Program.Provider.GetService<IRevitContext>());
#if DEBUG
            loggerConfiguration
                .WriteTo.Console()
                .WriteTo.Debug();
#endif
            Log.Logger = loggerConfiguration
                .WriteTo.File(
                    logPath,
                    rollingInterval: RollingInterval.Day,
                    fileSizeLimitBytes: LogFileSizeLimitBytes,
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: RetainedLogFileCountLimit)
                .CreateLogger();

            return services.AddSerilog(dispose: true);
        }
    }
}
