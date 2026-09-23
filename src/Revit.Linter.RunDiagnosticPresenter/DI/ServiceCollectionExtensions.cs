using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.RunDiagnosticPresenter.ViewModels;
using Revit.Linter.RunDiagnosticPresenter.Views;

namespace Revit.Linter.RunDiagnosticPresenter.DI;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
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
