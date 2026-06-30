using Microsoft.Extensions.DependencyInjection;
using MVVM.DependencyInjection;
using Revit.Linter.FixReportPresenter.Views;

namespace Revit.Linter.FixReportPresenter.DI;

public static class Registrator
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddFixReportPresenterModule()
            => services
                .AddView<FixReportView>(ServiceLifetime.Singleton)
        ;
    }
}
