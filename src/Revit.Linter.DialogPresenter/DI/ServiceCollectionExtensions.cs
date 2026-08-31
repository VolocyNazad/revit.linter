using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.DialogPresenter.Abstractions;
using Revit.Linter.DialogPresenter.ViewModels;

namespace Revit.Linter.DialogPresenter.DI;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddDialogModule()
            => services.AddTransient<IDialog, DialogViewModel>()
                .AddTransient<IConfirmationDialog, ConfirmationDialogViewModel>()
        ;
    }
}
