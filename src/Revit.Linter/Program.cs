using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Revit.Context.DI;
using Revit.Events.DI;
using Revit.Linter.CollisionDiagnostics.DI;
using Revit.Linter.ConfigurationPath.DI;
using Revit.Linter.Core.Abstractions;
using Revit.Linter.Diagnostic.DI;
using Revit.Linter.DiagnosticListPresenter.DI;
using Revit.Linter.DiagnosticReportExporting.DI;
using Revit.Linter.DiagnosticReportPresenter.DI;
using Revit.Linter.DiagnosticReportProvider.DI;
using Revit.Linter.DialogPresenter.DI;
using Revit.Linter.DocumentDiagnostics.DI;
using Revit.Linter.DocumentQueries.DI;
using Revit.Linter.ElementAccentor.DI;
using Revit.Linter.ElementChangesMonitor.DI;
using Revit.Linter.ElementChangesProvider.DI;
using Revit.Linter.ElementDiagnostics.DI;
using Revit.Linter.ElementFixing.DI;
using Revit.Linter.ElementIgnoring.DI;
using Revit.Linter.ElementVisualization.DI;
using Revit.Linter.FixReportPresenter.DI;
using Revit.Linter.FixReportProvider.DI;
using Revit.Linter.IgnoreListPresenter.DI;
using Revit.Linter.Infrastructure.Extensions;
using Revit.Linter.OpenedDocuments.DI;
using Revit.Linter.ParameterElementDiagnostics.DI;
using Revit.Linter.Presentation;
using Revit.Linter.ProjectParameterManaging.DI;
using Revit.Linter.RunDiagnosticPresenter.DI;
using Revit.Linter.ThemeManaging.DI;
using Revit.Linter.UserDiagnostics.DI;
using Revit.Linter.PracticalTourPresenter.DI;
using Revit.Linter.WelcomePresenter.DI;
using Revit.TransactionMemoryCache.DI;
using System.IO;
using System.Reflection;
using Toolkit.ValueStore.DI;
using Toolkit.ValueStore.Serialization;

namespace Revit.Linter;

internal sealed class Program
{
    private static readonly object _hostSync = new();
    private static IHost? _host;
    private static bool _shutdown;

    private Program() { }

    // The host is built once per Revit session. After Shutdown no service is available again: a failed
    // startup must not let a later call (for example the shutdown logger) rebuild the whole graph.
    private static IHost Host
    {
        get
        {
            lock (_hostSync)
            {
                if (_shutdown)
                    throw new ObjectDisposedException(nameof(Program), "The Revit Linter host has been shut down.");

                return _host ??= CreateHostBuilder(Environment.GetCommandLineArgs()).Build();
            }
        }
    }

    public static IServiceProvider Provider
    {
        get
        {
            IServiceProvider provider = Host.Services;
            ViewLocatorExtension.Initialize(provider);
            return provider;
        }
    }
    private static string Location => field ??= Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
        ?? throw new InvalidOperationException("The Revit Linter host location is unavailable.");

    internal static void Shutdown()
    {
        IHost? host;
        lock (_hostSync)
        {
            if (_shutdown) return;

            _shutdown = true;
            host = _host;
            _host = null;
        }

        host?.Dispose();
    }

    /// <summary>
    /// Resolves a service only while the host is alive, without building it from a background caller.
    /// </summary>
    /// <typeparam name="T">The service type to resolve.</typeparam>
    /// <returns>The service, or <see langword="null"/> before the host is built and after it is shut down.</returns>
    /// <remarks>
    /// Log enrichers run on arbitrary threads, including during host building and disposal, so they use this
    /// probe instead of <see cref="Provider"/>, which intentionally builds the host on first use.
    /// </remarks>
    internal static T? TryGetService<T>() where T : class
    {
        IHost? host;
        lock (_hostSync)
        {
            if (_shutdown) return null;
            host = _host;
        }

        return host?.Services.GetService<T>();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
            .UseContentRoot(Location)
            .ConfigureLogging(logging => logging.ClearProviders())
            .ConfigureAppConfiguration((_, cfg) => cfg
                .SetBasePath(Location))
            .ConfigureServices((context, services) => services
                .AddAndConfigureSerilog()
                .AddLinterHostServices()
                .AddRevitContext().AddEvents().AddTransactionMemoryCache().AddDocumentQueries()
                .AddElementAccentor().AddElementFixing().AddElementVisualization()
                .AddConfigurationPathModule()
                .AddDiagnosticModule().AddElementChangesMonitorModule().AddElementIgnoringModule().AddProjectParameterManagingModule()
                .AddElementDiagnostics().AddDocumentDiagnostics()
                .AddUserDiagnostics()
                .AddCollisionDiagnostics()
                .AddParameterElementDiagnostics()
                .AddDiagnosticReportProviderModule().AddFixReportProviderModule().AddElementChangesProviderModule()
                .AddRunDiagnosticModule().AddDiagnosticReportExportingModule().AddDiagnosticReportPresenterModule()
                .AddDiagnosticListPresenterModule().AddFixReportPresenterModule().AddIgnoreListPresenterModule().AddDialogModule()
                .AddWelcomePresenterModule()
                .AddPracticalTourPresenterModule()
                .AddOpenedDocumentsModule()
                .AddThemeManagingModule()
                .AddValueStore<YamlValueStoreSerializer>(options =>
                {
                    options.DirectoryPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        ProductIdentity.CompanyDirectoryName,
                        ProductIdentity.ProductDirectoryName,
                        ProductIdentity.SettingsFolderName);
                    options.WatchForExternalChanges = true;
                })
            )
        ;
}
