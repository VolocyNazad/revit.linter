using Microsoft.Extensions.DependencyInjection;
using MVVM.DependencyInjection;
using Revit.Linter.DiagnosticReportPresenter.Interactions.Abstractions.Services;
using Revit.Linter.DiagnosticReportPresenter.Exporting;
using Revit.Linter.DiagnosticReportPresenter.ViewModels;
using Revit.Linter.DiagnosticReportPresenter.Views;

namespace Revit.Linter.DiagnosticReportPresenter.DI;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddDiagnosticReportPresenterModule()
            => services.AddView<DiagnosticReportView>(ServiceLifetime.Singleton)
                .AddSingleton<IDiagnosticReportPresenter>(provider => provider.GetRequiredService<DiagnosticReportViewModel>())
                .AddSingleton<IDiagnosticReportExporter, CsvDiagnosticReportExporter>()
                .AddSingleton<IDiagnosticReportExporter, JsonDiagnosticReportExporter>()
                .AddSingleton<IDiagnosticReportExporter, YamlDiagnosticReportExporter>()
                .AddSingleton<IDiagnosticReportExporter, HtmlDiagnosticReportExporter>()
        ;
    }
}
