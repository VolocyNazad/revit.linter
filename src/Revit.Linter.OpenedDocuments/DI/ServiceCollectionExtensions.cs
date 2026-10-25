using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.OpenedDocuments.Abstractions.Services;
using Revit.Linter.OpenedDocuments.ViewModels;

namespace Revit.Linter.OpenedDocuments.DI;

/// <summary>
/// Registers services that expose the documents currently open in Revit.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds and initializes the shared opened-documents view model.
        /// </summary>
        /// <returns>The same service collection so that additional registrations can be chained.</returns>
        /// <remarks>
        /// The instance is registered under <see cref="IOpenedDocuments"/> as well, so presenters depend on the
        /// selector contract instead of its view model.
        /// </remarks>
        public IServiceCollection AddOpenedDocumentsModule()
            => services
                .AddSingleton(provider =>
                {
                    var service = ActivatorUtilities.CreateInstance<OpenedDocumentsViewModel>(provider);
                    _ = service.Initialize();
                    return service;
                })
                .AddSingleton<IOpenedDocuments>(provider => provider.GetRequiredService<OpenedDocumentsViewModel>())
        ;
    }
}
