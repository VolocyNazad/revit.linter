using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.DiagnosticListPresenter.Views;
using MVVM.DependencyInjection;
using Revit.Linter.DiagnosticListPresenter.ViewModels;

namespace Revit.Linter.DiagnosticListPresenter.DI;

public static class Registrator
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddDiagnosticListPresenterModule()
            => services
                .AddView<DiagnosticListView>(ServiceLifetime.Singleton)
                .AddTransient<DiagnosticItemViewModel>()
        ;
    }
}
