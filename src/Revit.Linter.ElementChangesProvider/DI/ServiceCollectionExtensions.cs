using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.ElementChangesProvider.Abstractions.Services;
using Provider = Revit.Linter.ElementChangesProvider.Services.ElementChangesProvider;

namespace Revit.Linter.ElementChangesProvider.DI;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddElementChangesProviderModule() => services
           .AddSingleton<Provider>()
           .AddSingleton<IElementChangesReceiver>(provider => provider.GetRequiredService<Provider>())
           .AddSingleton<IElementChangesSender>(provider => provider.GetRequiredService<Provider>())
       ;
    }
}
