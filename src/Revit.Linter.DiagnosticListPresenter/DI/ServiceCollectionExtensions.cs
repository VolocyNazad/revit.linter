using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.DiagnosticListPresenter.Views;
using MVVM.DependencyInjection;
using Revit.Linter.DiagnosticListPresenter.ViewModels;

namespace Revit.Linter.DiagnosticListPresenter.DI;

/// <summary>
/// Registers the diagnostic-list presentation module.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds the shared diagnostic-list view and transient diagnostic item view models.
        /// </summary>
        /// <returns>The same service collection so that additional registrations can be chained.</returns>
        public IServiceCollection AddDiagnosticListPresenterModule()
            => services
                .AddView<DiagnosticListView>(ServiceLifetime.Singleton)
                .AddTransient<DiagnosticItemViewModel>()
        ;
    }
}
