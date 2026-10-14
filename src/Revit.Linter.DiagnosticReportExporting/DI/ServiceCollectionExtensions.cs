using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.DiagnosticReportExporting.Abstractions.Services;
using Revit.Linter.DiagnosticReportExporting.Infrastructure.Services;

namespace Revit.Linter.DiagnosticReportExporting.DI;

/// <summary>
/// Provides dependency-injection registration for the diagnostic report exporting.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the report exporters, the save-file dialog and the export orchestration.
        /// </summary>
        /// <returns>The service collection for registration chaining.</returns>
        public IServiceCollection AddDiagnosticReportExportingModule() => services
                .AddSingleton<IDiagnosticReportExporter, CsvDiagnosticReportExporter>()
                .AddSingleton<IDiagnosticReportExporter, JsonDiagnosticReportExporter>()
                .AddSingleton<IDiagnosticReportExporter, YamlDiagnosticReportExporter>()
                .AddSingleton<IDiagnosticReportExporter, HtmlDiagnosticReportExporter>()
                .AddSingleton<ISaveFileDialogService, SaveFileDialogService>()
                .AddSingleton<IDiagnosticReportExportService, DiagnosticReportExportService>()
            ;
    }
}
