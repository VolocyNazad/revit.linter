using Revit.Linter.CollisionDiagnostics.Abstractions.Infrasructure.Services;
using Revit.Linter.CollisionDiagnostics.Infrasructure.Extensions;
using Revit.TransactionMemoryCache.Abstractions.Services;

namespace Revit.Linter.CollisionDiagnostics.Infrasructure.Services;

internal sealed class GetElementBoundingBoxService(IRevitTransactionMemoryCache revitTransactionMemoryCache) : IGetElementBoundingBoxService
{
    private static readonly Options _defaultOptions = new();

    public BoundingBoxXYZ Execute(Element element, View? view)
        => revitTransactionMemoryCache.GetOrCreate(
            $"element:b-box:id:{element.Id.Value()}",
            () => element.get_Geometry(view is null ? _defaultOptions : new() { View = view }).GetBoundingBox()) 
        ?? throw new InvalidOperationException($"Failed to get object from cache.");

#if BEFORE2024
    public BoundingBoxXYZ Execute(int elementId, GeometryElement geometryElement)
#else
    public BoundingBoxXYZ Execute(long elementId, GeometryElement geometryElement)
#endif
        => revitTransactionMemoryCache.GetOrCreate(
            $"element:b-box:id:{elementId}",
            () => geometryElement.GetBoundingBox()) 
        ?? throw new InvalidOperationException($"Failed to get object from cache.");
}
