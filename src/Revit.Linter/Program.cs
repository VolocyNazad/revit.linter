using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Revit.Context.DI;
using Revit.Events.DI;
using Revit.Linter.CollisionDiagnostics.DI;
using Revit.Linter.Diagnostic.DI;
using Revit.Linter.DiagnosticListPresenter.DI;
using Revit.Linter.DiagnosticReportPresenter.DI;
using Revit.Linter.DiagnosticReportProvider.DI;
using Revit.Linter.DialogPresenter.DI;
using Revit.Linter.DocumentDiagnostics.DI;
using Revit.Linter.ElementAccentor.DI;
using Revit.Linter.ElementChangesMonitor.DI;
using Revit.Linter.ElementChangesProvider.DI;
using Revit.Linter.ElementDiagnostics.DI;
using Revit.Linter.ElementIgnoring.DI;
using Revit.Linter.FixReportPresenter.DI;
using Revit.Linter.FixReportProvider.DI;
using Revit.Linter.Infrastructure.Exceptions;
using Revit.Linter.Infrastructure.Extensions;
using Revit.Linter.Infrastructure.Services;
using Revit.Linter.OpenedDocuments.DI;
using Revit.Linter.ParameterElementDiagnostics.DI;
using Revit.Linter.ProjectParameterManaging.DI;
using Revit.Linter.RunDiagnosticPresenter.DI;
using Revit.Linter.ThemeManaging.DI;
using Revit.Linter.UserDiagnostics.DI;
using Revit.Linter.ValueStore.DI;
using Revit.Linter.WarningsHandling.DI;
using Revit.TransactionMemoryCache.DI;
using System.IO;
using System.Reflection;

namespace Revit.Linter;

internal sealed class Program
{
    private Program() { }

    private static IHost Host => field ??= CreateHostBuilder(Environment.GetCommandLineArgs()).Build();
    public static IServiceProvider Provider => Host.Services;
    private static string Location => field ??= Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
        ?? throw new HostLocationNotFoundException();

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
            .UseContentRoot(Location)
            .ConfigureLogging(logging => logging.ClearProviders())
            .ConfigureAppConfiguration((_, cfg) => cfg
                .SetBasePath(Location))
#if (DEBUG)
            .UseEnvironment("Production")
            //.UseEnvironment("Development")
#endif
            .ConfigureServices((context, services) => services
                .AddAndConfigureSerilog()
                .AddLocalization(i => i.ResourcesPath = "Resources")
                .AddRevitContext().AddEvents().AddTransactionMemoryCache().AddElementAccentor()
                .AddDiagnosticModule().AddElementChangesMonitorModule().AddRevitWarningsModule().AddElementIgnoringModule().AddProjectParameterManagingModule()
                .AddElementDiagnostics().AddDocumentDiagnostics()
                .AddUserDiagnostics()
                .AddCollisionDiagnostics()
                .AddParameterElementDiagnostics()
                .AddDiagnosticReportProviderModule().AddFixReportProviderModule().AddElementChangesProviderModule()
                .AddRunDiagnosticModule().AddDiagnosticReportPresenterModule()
                .AddDiagnosticListPresenterModule().AddFixReportPresenterModule().AddDialogModule()
                .AddSingleton<DiagnosticCatalogNotifier>()
                .AddSingleton<ValueStoreNotifier>()
                .AddOpenedDocumentsModule()
                .AddThemeManagingModule()
                .AddValueStoreModule()
            )
        ;

}
