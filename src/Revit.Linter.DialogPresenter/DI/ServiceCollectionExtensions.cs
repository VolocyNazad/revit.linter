using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.DialogPresenter.Abstractions;
using Revit.Linter.DialogPresenter.ViewModels;

namespace Revit.Linter.DialogPresenter.DI;

/// <summary>
/// Registers informational and confirmation dialog services.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds transient informational and confirmation dialog services.
        /// </summary>
        /// <returns>The same service collection so that additional registrations can be chained.</returns>
        public IServiceCollection AddDialogModule()
            => services.AddTransient<IDialog, DialogViewModel>()
                .AddTransient<IConfirmationDialog, ConfirmationDialogViewModel>()
        ;
    }
}
