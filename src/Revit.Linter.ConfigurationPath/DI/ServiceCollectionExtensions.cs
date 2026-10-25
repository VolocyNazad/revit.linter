using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.ConfigurationPath.Abstractions.Services;
using Revit.Linter.ConfigurationPath.Services;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ConfigurationPath.DI;

/// <summary>
/// Registers the diagnostic configuration files, their shared state and their writer.
/// </summary>
/// <remarks>
/// The module owns both the configuration cache and the change source, so consumers receive the same
/// singleton through the contract they need instead of resolving a concrete type from the root provider.
/// </remarks>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds the diagnostic configuration services to the application service collection.</summary>
        /// <returns>The same service collection so that additional registrations can be chained.</returns>
        public IServiceCollection AddConfigurationPathModule() => services
            .AddSingleton<DiagnosticConfigurationErrorState>()
            .AddSingleton<IDiagnosticConfigurationErrorState>(provider =>
                provider.GetRequiredService<DiagnosticConfigurationErrorState>())
            .AddSingleton<IDiagnosticConfigurationErrorSource>(provider =>
                provider.GetRequiredService<DiagnosticConfigurationErrorState>())
            .AddSingleton<IDiagnosticConfigurationParseErrorSource>(provider =>
                provider.GetRequiredService<DiagnosticConfigurationErrorState>())
            .AddSingleton<IDiagnosticConfigurationProblemSource>(provider =>
                provider.GetRequiredService<DiagnosticConfigurationErrorState>())
            .AddSingleton<IConfigurationPaths, ConfigurationPaths>()
            .AddSingleton<DiagnosticConfigurationService>(provider => new DiagnosticConfigurationService(
                provider.GetRequiredService<DiagnosticConfigurationErrorState>(),
                provider.GetRequiredService<IConfigurationPaths>(),
                provider.GetService<IDiagnosticConfigurationRuleTextFormatter>()))
            .AddSingleton<IDiagnosticConfigurationService>(provider =>
                provider.GetRequiredService<DiagnosticConfigurationService>())
            .AddSingleton<IDiagnosticCatalogChangeSource>(provider =>
                provider.GetRequiredService<DiagnosticConfigurationService>())
            .AddSingleton<BuiltInDiagnosticStateProvider>()
            .AddSingleton<IBuiltInDiagnosticStateProvider>(provider =>
                provider.GetRequiredService<BuiltInDiagnosticStateProvider>())
            .AddSingleton<DiagnosticConfigurationWriter>()
            .AddSingleton<IDiagnosticConfigurationWriter>(provider =>
                provider.GetRequiredService<DiagnosticConfigurationWriter>())
        ;
    }
}
