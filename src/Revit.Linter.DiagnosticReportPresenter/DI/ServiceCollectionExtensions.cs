using Microsoft.Extensions.DependencyInjection;
using MVVM.DependencyInjection;
using Revit.Linter.DiagnosticReportPresenter.Interactions.Abstractions.Services;
using Revit.Linter.DiagnosticReportPresenter.ViewModels;
using Revit.Linter.DiagnosticReportPresenter.Views;

namespace Revit.Linter.DiagnosticReportPresenter.DI;

/// <summary>
/// Provides dependency-injection registration for the diagnostic report presenter.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the diagnostic report view and presenter.
        /// </summary>
        /// <returns>The service collection for registration chaining.</returns>
        public IServiceCollection AddDiagnosticReportPresenterModule()
            => services.AddView<DiagnosticReportView>(ServiceLifetime.Singleton)
                .AddSingleton<IDiagnosticReportPresenter>(provider => provider.GetRequiredService<DiagnosticReportViewModel>())
        ;
    }
}
