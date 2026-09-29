using Microsoft.Extensions.DependencyInjection;
using MVVM.DependencyInjection;
using Revit.Linter.FixReportPresenter.Views;

namespace Revit.Linter.FixReportPresenter.DI;

/// <summary>
/// Registers the fix-report presentation module.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds the shared fix-report WPF view and its view model.
        /// </summary>
        /// <returns>The same service collection so that additional registrations can be chained.</returns>
        public IServiceCollection AddFixReportPresenterModule()
            => services
                .AddView<FixReportView>(ServiceLifetime.Singleton)
        ;
    }
}
