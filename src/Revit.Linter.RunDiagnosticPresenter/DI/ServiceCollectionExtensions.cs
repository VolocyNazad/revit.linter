using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.RunDiagnosticPresenter.ViewModels;
using Revit.Linter.RunDiagnosticPresenter.Views;

namespace Revit.Linter.RunDiagnosticPresenter.DI;

/// <summary>
/// Registers the diagnostic-run presentation services and view composition.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds the shared diagnostic-run view model and its transient WPF view.
        /// </summary>
        /// <returns>The same service collection so that additional registrations can be chained.</returns>
        public IServiceCollection AddRunDiagnosticModule()
            => services
                .AddSingleton<RunDiagnosticViewModel>()
                .AddTransient(provider => new RunDiagnosticView
                {
                    DataContext = provider.GetRequiredService<RunDiagnosticViewModel>()
                })
        ;
    }
}
