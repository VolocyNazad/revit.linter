using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Revit.Linter.ConfigurationPath.Abstractions.Services;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.DiagnosticReportPresenter.Interactions.Abstractions.Services;
using Revit.Linter.Infrastructure.Services;
using Revit.Linter.RunDiagnosticPresenter.Abstractions;
using Revit.Linter.WelcomePresenter.Abstractions;
using Revit.Linter.WelcomePresenter.Abstractions.Services;

namespace Revit.Linter.Infrastructure.Extensions;

/// <summary>
/// Registers the services owned by the add-in host rather than by a feature module.
/// </summary>
/// <remarks>
/// Host-owned services adapt Revit UI, the file system, dialogs and documentation for the modules.
/// They stay in the host composition because no feature module may own them.
/// </remarks>
internal static class HostServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds the host-owned services to the application service collection.</summary>
        /// <returns>The same service collection so that additional registrations can be chained.</returns>
        public IServiceCollection AddLinterHostServices() => services
            .AddSingleton<IStringLocalizer<GlobalLocalizations>, GlobalStringLocalizer>()
            .AddSingleton<IDiagnosticConfigurationRuleTextFormatter, GlobalDiagnosticConfigurationRuleTextFormatter>()
            .AddSingleton<WelcomeHost>()
            .AddSingleton<IWelcomeHost>(provider => provider.GetRequiredService<WelcomeHost>())
            .AddSingleton<IPracticalTourHost>(provider => provider.GetRequiredService<WelcomeHost>())
            .AddSingleton<ITutorialSampleCatalog, AutodeskTutorialSampleCatalog>()
            .AddSingleton<TutorialSampleOpenRequest>()
            .AddSingleton<IDocumentationLauncher, DocumentationLauncher>()
            .AddSingleton<IShellLauncher, ShellLauncher>()
            .AddSingleton<IClipboardService, ClipboardService>()
            .AddSingleton<IUserInterfaceActivityStream, UserInterfaceActivityStream>()
            .AddSingleton<RevitEventSource>()
            .AddSingleton<IRevitEventSource>(provider => provider.GetRequiredService<RevitEventSource>())
            .AddSingleton<IDiagnosticReportPaneActivator, DiagnosticReportPaneActivator>()
            .AddSingleton<IVisualizationViewActivator, VisualizationViewActivator>()
            .AddSingleton<DiagnosticCatalogNotifier>()
            .AddSingleton<ConfigurationMigrationNotice>()
            .AddSingleton<ValueStoreNotifier>()
            .AddSingleton<RevitIdlingScheduler>()
            .AddSingleton<IRevitIdlingScheduler>(provider => provider.GetRequiredService<RevitIdlingScheduler>())
            .AddSingleton<IFormulaCompilationNotifier, FormulaCompilationNotifier>()
        ;
    }
}
