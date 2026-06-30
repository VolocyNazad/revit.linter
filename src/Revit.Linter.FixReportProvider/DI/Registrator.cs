using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.FixReportProvider.Abstractions.Services;
using ReportProvider = Revit.Linter.FixReportProvider.Services.FixReportProvider;

namespace Revit.Linter.FixReportProvider.DI;

public static class Registrator
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddFixReportProviderModule() => services
           .AddSingleton<ReportProvider>()
           .AddSingleton<IFixReportReceiver>(provider => provider.GetRequiredService<ReportProvider>())
           .AddSingleton<IFixReportSender>(provider => provider.GetRequiredService<ReportProvider>())
       ;
    }
}
