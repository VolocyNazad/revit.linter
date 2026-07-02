using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.OpenedDocuments.ViewModels;

namespace Revit.Linter.OpenedDocuments.DI;

public static class Registrator
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddOpenedDocumentsModule()
            => services.AddSingleton(provider =>
            {
                var service = ActivatorUtilities.CreateInstance<OpenedDocumentsViewModel>(provider);
                service.Initialize();
                return service;
            })
        ;
    }
}
