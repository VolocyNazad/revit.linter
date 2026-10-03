using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.DocumentQueries.Abstractions.Services;
using Revit.Linter.DocumentQueries.Infrastructure.Services;

namespace Revit.Linter.DocumentQueries.DI;

/// <summary>
/// Provides dependency-injection registration for cached document queries.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the cached document query service.
        /// </summary>
        /// <returns>The service collection for registration chaining.</returns>
        /// <remarks>The transaction memory cache must be registered separately.</remarks>
        public IServiceCollection AddDocumentQueries()
            => services.AddSingleton<IDocumentQueryService, DocumentQueryService>();
    }
}