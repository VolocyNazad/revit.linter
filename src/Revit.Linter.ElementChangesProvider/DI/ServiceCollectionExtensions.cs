using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.ElementChangesProvider.Abstractions.Services;
using Provider = Revit.Linter.ElementChangesProvider.Services.ElementChangesProvider;

namespace Revit.Linter.ElementChangesProvider.DI;

/// <summary>
/// Registers element-change notification services.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds the shared element-changes provider as both a sender and a receiver.
        /// </summary>
        /// <returns>The same service collection so that additional registrations can be chained.</returns>
        public IServiceCollection AddElementChangesProviderModule() => services
           .AddSingleton<Provider>()
           .AddSingleton<IElementChangesReceiver>(provider => provider.GetRequiredService<Provider>())
           .AddSingleton<IElementChangesSender>(provider => provider.GetRequiredService<Provider>())
       ;
    }
}
