using Revit.Linter.CollisionDiagnostics.Abstractions.Infrastructure.Services;
using Revit.Linter.CollisionDiagnostics.Infrastructure.Extensions;
using Toolkit.Revit.Extensions;
using Revit.TransactionMemoryCache.Abstractions.Services;

namespace Revit.Linter.CollisionDiagnostics.Infrastructure.Services;

internal sealed class GetElementGeometryService(IRevitTransactionMemoryCache revitTransactionMemoryCache) : IGetElementGeometryService
{
    private static readonly Options _defaultOptions = new();

    public IReadOnlyCollection<Solid> Execute(Element element, View? view)
        => revitTransactionMemoryCache.GetOrCreate(
            $"element:geometry:id:{element.Id.Value()}",
            () => element.GetSolids(view is null ? _defaultOptions : new() { View = view })) ?? throw new InvalidOperationException($"Failed to get object from cache.");

#if BEFORE2024
    public IReadOnlyCollection<Solid> Execute(int elementId, GeometryElement geometryElement)
#else
    public IReadOnlyCollection<Solid> Execute(long elementId, GeometryElement geometryElement)
#endif
        => revitTransactionMemoryCache.GetOrCreate(
            $"element:geometry:id:{elementId}",
            () => geometryElement.GetSolids()) ?? throw new InvalidOperationException($"Failed to get object from cache.");
}
