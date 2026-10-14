using Microsoft.Extensions.DependencyInjection;
using MVVM.DependencyInjection;
using Revit.Linter.IgnoreListPresenter.Views;

namespace Revit.Linter.IgnoreListPresenter.DI;

/// <summary>
/// Registers the ignore-list presentation module.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds the shared ignore-list WPF view and its view model.
        /// </summary>
        /// <returns>The same service collection so that additional registrations can be chained.</returns>
        public IServiceCollection AddIgnoreListPresenterModule()
            => services
                .AddView<IgnoreListView>(ServiceLifetime.Singleton)
        ;
    }
}
