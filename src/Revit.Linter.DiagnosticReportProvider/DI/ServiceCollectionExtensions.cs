using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Services;
using ReportProvider = Revit.Linter.DiagnosticReportProvider.Services.DiagnosticReportProvider;

namespace Revit.Linter.DiagnosticReportProvider.DI;

/// <summary>
/// Registers diagnostic-report publishing services.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds the shared diagnostic-report provider as both a sender and a receiver.
        /// </summary>
        /// <returns>The same service collection so that additional registrations can be chained.</returns>
        public IServiceCollection AddDiagnosticReportProviderModule() => services
           .AddSingleton<ReportProvider>()
           .AddSingleton<IDiagnosticReportReceiver>(provider => provider.GetRequiredService<ReportProvider>())
           .AddSingleton<IDiagnosticReportSender>(provider => provider.GetRequiredService<ReportProvider>())
       ;
    }
}
