using Microsoft.Extensions.DependencyInjection;
using MVVM.DependencyInjection;
using Revit.Linter.RunDiagnosticPresenter.Views;

namespace Revit.Linter.RunDiagnosticPresenter.DI;

public static class Registrator
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddRunDiagnosticModule()
            => services.AddView<RunDiagnosticView>(ServiceLifetime.Singleton)
        ;
    }
}
