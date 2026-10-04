using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Revit.Linter.Languages.Factories;

namespace Revit.Linter.Languages.DI;

/// <summary>
/// Provides dependency-injection registration for the formula language.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the factories that compile configuration formulas.
        /// </summary>
        /// <returns>The service collection so that additional registrations can be chained.</returns>
        /// <remarks>Every diagnostic module calls this method; the factories are registered once.</remarks>
        public IServiceCollection AddFormulaFactories()
        {
            services.TryAddSingleton<DocumentFilterFactory>();
            services.TryAddSingleton<ElementFilterFactory>();
            services.TryAddSingleton<ElementFunctionFactory>();
            return services;
        }
    }
}
