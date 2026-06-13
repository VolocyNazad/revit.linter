using Revit.Linter.CollisionDiagnostics.Abstractions.Infrasructure.Services;
using Revit.Linter.CollisionDiagnostics.Infrasructure.Extensions;
using Revit.TransactionMemoryCache.Abstractions.Services;

namespace Revit.Linter.CollisionDiagnostics.Infrasructure.Services;

internal sealed class GetElementGeomentryService(IRevitTransactionMemoryCache revitTransactionMemoryCache) : IGetElementGeomentryService
{
    private static readonly Options _defaultOptions = new();

    public ICollection<Solid> Execute(Element element, View? view)
        => revitTransactionMemoryCache.GetOrCreate(
            $"element:geometry:id:{element.Id.Value()}",
            () => element.GetSolids(view is null ? _defaultOptions : new() { View = view })) ?? throw new InvalidOperationException($"Failed to get object from cache.");

#if BEFORE2024
    public ICollection<Solid> Execute(int elementId, GeometryElement geometryElement)
#else
    public ICollection<Solid> Execute(long elementId, GeometryElement geometryElement)
#endif
        => revitTransactionMemoryCache.GetOrCreate(
            $"element:geometry:id:{elementId}",
            () => geometryElement.GetSolids()) ?? throw new InvalidOperationException($"Failed to get object from cache.");
}