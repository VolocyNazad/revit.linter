using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Revit.Linter.Infrastructure.Extensions;

internal static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddAndConfigureSerilog()
        {
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                ?? throw new InvalidOperationException("Log path is null");
            string logPath = Path.Combine(path, "logs.txt");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.WithProperty("isDebug", Debugger.IsAttached)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .WriteTo.Console()
                .WriteTo.Debug()
                .WriteTo.File(logPath)
                .CreateLogger();

            return services.AddSerilog(); // todo add revit context enricher
        }
    }
}
