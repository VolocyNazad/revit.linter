using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Revit.Context.DI;
using Revit.Linter.BackgroundDiagnostic.DI;
using Revit.Linter.CollisionDiagnostics.DI;
using Revit.Linter.Diagnostic.DI;
using Revit.Linter.DiagnosticListPresenter.DI;
using Revit.Linter.DiagnosticReportPresenter.DI;
using Revit.Linter.DiagnosticReportProvider.DI;
using Revit.Linter.DocumentDiagnostics.DI;
using Revit.Linter.ElementAccentor.DI;
using Revit.Linter.ElementDiagnostics.DI;
using Revit.Linter.Infrastructure.Exceptions;
using Revit.Linter.ParameterElementDiagnostics.DI;
using Revit.Linter.UserDiagnostics.DI;
using Revit.TransactionMemoryCache.DI;
using System.IO;
using System.Reflection;

namespace Revit.Linter;

internal sealed class Program
{
    private Program() { }

    public static IHost Host => field ??= CreateHostBuilder(Environment.GetCommandLineArgs()).Build();
    public static IServiceProvider Provider => Host.Services;
    private static string Location => field ??= Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
        ?? throw new HostLocationNotFoundException();

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
            .UseContentRoot(Location)
            .ConfigureAppConfiguration((_, cfg) => cfg
                .SetBasePath(Location))
#if (DEBUG)
            .UseEnvironment("Production")
            //.UseEnvironment("Development")
#endif
            .ConfigureServices((_, services) => services
                .AddLocalization(i => i.ResourcesPath = "Resources")
                .AddRevitContext().AddTransactionMemoryCache().AddElementAccentor()
                .AddDiagnosticModule().AddBackgroundDiagnosticModule()
                .AddElementDiagnostics().AddDocumentDiagnostics()
                .AddUserDiagnostics()
                .AddCollisionDiagnostics()
                .AddParameterElementDiagnostics()
                .AddDiagnosticReportProviderModule()
                .AddDiagnosticReportPresenterModule().AddDiagnosticListPresenterModule())
        ;

}
