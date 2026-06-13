using Microsoft.Extensions.DependencyInjection;
using MVVM.DependencyInjection;
using Revit.Linter.DiagnosticReportPresenter.Views;

namespace Revit.Linter.DiagnosticReportPresenter.DI;

public static class Registrator
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddDiagnosticReportPresenterModule()
            => services.AddView<DiagnosticReportView>(ServiceLifetime.Singleton)
        ;
    }
}
