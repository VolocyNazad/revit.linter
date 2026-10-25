using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.PracticalTourPresenter.Infrastructure.Services;
using Revit.Linter.PracticalTourPresenter.Interactions.Abstractions;
using Revit.Linter.PracticalTourPresenter.Views;

namespace Revit.Linter.PracticalTourPresenter.DI;

/// <summary>Registers the practical-tour presenter module.</summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds the practical tour, its managed configuration installer and its pane view.</summary>
        /// <returns>The same service collection so that additional registrations can be chained.</returns>
        /// <remarks>The host must also register <c>IPracticalTourHost</c> and the settings value store.</remarks>
        public IServiceCollection AddPracticalTourPresenterModule()
            => services
                .AddSingleton<PracticalTourConfigurationInstaller>()
                .AddSingleton<PracticalTourView>()
                .AddSingleton<IPracticalTour, PracticalTour>();
    }
}
