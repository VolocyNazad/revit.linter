using Revit.Linter.DocumentQueries.Abstractions.Services;

namespace Revit.Linter.CollisionDiagnostics.Infrastructure.Extensions;

internal static class DocumentQueryServiceExtensions
{
    // Collision checks need geometry for every element they take, so a missing geometry fails the rule
    // instead of silently skipping the element.
    public static GeometryElement GetRequiredGeometry(
        this IDocumentQueryService documentQueries, Element element, View? view)
        => documentQueries.GetGeometry(element, view)
           ?? throw new InvalidOperationException($"Element {element.Id} has no geometry.");
}