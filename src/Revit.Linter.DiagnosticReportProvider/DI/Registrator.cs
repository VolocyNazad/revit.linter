using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Services;
using ReportProvider = Revit.Linter.DiagnosticReportProvider.Services.DiagnosticReportProvider;

namespace Revit.Linter.DiagnosticReportProvider.DI;

public static class Registrator
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddDiagnosticReportProviderModule() => services
           .AddSingleton<ReportProvider>()
           .AddSingleton<IDiagnosticReportReceiver>(provider => provider.GetRequiredService<ReportProvider>())
           .AddSingleton<IDiagnosticReportSender>(provider => provider.GetRequiredService<ReportProvider>())
       ;
    }
}
