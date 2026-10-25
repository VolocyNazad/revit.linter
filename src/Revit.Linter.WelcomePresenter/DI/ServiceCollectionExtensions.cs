using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.WelcomePresenter.Abstractions;
using Revit.Linter.WelcomePresenter.Abstractions.Services;
using Revit.Linter.WelcomePresenter.Infrastructure.Services;
using Revit.Linter.WelcomePresenter.ViewModels;

namespace Revit.Linter.WelcomePresenter.DI;

/// <summary>
/// Registers the welcome wizard module.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds the welcome wizard, its steps and the example configuration installer.
        /// </summary>
        /// <returns>The same service collection so that additional registrations can be chained.</returns>
        /// <remarks>
        /// The host must also register <see cref="IWelcomeHost"/> and the settings value store. The practical
        /// tour registers itself through the practical-tour presenter module.
        /// The wizard and its steps are transient, so every show starts from a clean state.
        /// </remarks>
        public IServiceCollection AddWelcomePresenterModule()
            => services
                .AddSingleton<IExampleConfigurationInstaller, ExampleConfigurationInstaller>()
                .AddSingleton<ITutorialSampleCopyService, TutorialSampleCopyService>()
                .AddTransient<WelcomeIntroStepViewModel>()
                .AddTransient<WelcomeRulesStepViewModel>()
                .AddTransient<WelcomeExamplesStepViewModel>()
                .AddTransient<WelcomePracticalStepViewModel>()
                .AddTransient<WelcomeFinishStepViewModel>()
                .AddTransient<WelcomeWindowFactory>()
                .AddTransient<IWelcomeWizard, WelcomeViewModel>()
        ;
    }
}
