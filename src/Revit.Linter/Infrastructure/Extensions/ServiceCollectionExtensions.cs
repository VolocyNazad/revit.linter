using Microsoft.Extensions.DependencyInjection;
using Revit.Context.Abstractions.Services;
using Revit.Linter.SerilogEnrichers;
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
                .Enrich.WithRevitContext(() => Program.Provider.GetService<IRevitContext>())
                .WriteTo.Console()
                .WriteTo.Debug()
                .WriteTo.File(logPath)
                .CreateLogger();

            return services.AddSerilog();
        }
    }
}
