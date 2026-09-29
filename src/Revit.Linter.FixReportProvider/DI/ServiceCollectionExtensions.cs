using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.FixReportProvider.Abstractions.Services;
using ReportProvider = Revit.Linter.FixReportProvider.Services.FixReportProvider;

namespace Revit.Linter.FixReportProvider.DI;

/// <summary>
/// Registers fix-report publishing services.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds the shared fix-report provider as both a sender and a receiver.
        /// </summary>
        /// <returns>The same service collection so that additional registrations can be chained.</returns>
        public IServiceCollection AddFixReportProviderModule() => services
           .AddSingleton<ReportProvider>()
           .AddSingleton<IFixReportReceiver>(provider => provider.GetRequiredService<ReportProvider>())
           .AddSingleton<IFixReportSender>(provider => provider.GetRequiredService<ReportProvider>())
       ;
    }
}
