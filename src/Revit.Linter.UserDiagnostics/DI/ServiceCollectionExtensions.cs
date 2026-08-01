using Microsoft.Extensions.DependencyInjection;

namespace Revit.Linter.UserDiagnostics.DI;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddUserDiagnostics()
        {
            services.AddSingleton<ElementFilterFactory>()
                .AddSingleton<ElementFunctionFactory>()
                .AddSingleton<DocumentFilterFactory>()
                .AddSingleton<IDiagnosticRegistrationProvider, UserDiagnosticRegistrationProvider>();
            return services;
        }
    }
}
