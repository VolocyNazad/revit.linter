using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.DialogPresenter.Abstractions;
using Revit.Linter.DialogPresenter.ViewModels;

namespace Revit.Linter.DialogPresenter.DI;

public static class Registrator
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddDialogModule()
            => services.AddTransient<IDialog, DialogViewModel>()
        ;
    }
}
