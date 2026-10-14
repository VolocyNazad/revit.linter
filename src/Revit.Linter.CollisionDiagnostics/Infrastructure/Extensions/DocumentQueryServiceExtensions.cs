using Revit.Linter.DocumentQueries.Abstractions.Services;

namespace Revit.Linter.CollisionDiagnostics.Infrastructure.Extensions;

internal static class DocumentQueryServiceExtensions
{
    // Guards the bounding-box path: elements without geometry are skipped upstream (they yield
    // no solids and stay out of the collision groups), so a missing geometry here means
    // the rule selected an element it cannot visualize or compare, and the rule must fail loudly.
    public static GeometryElement GetRequiredGeometry(
        this IDocumentQueryService documentQueries, Element element, View? view)
        => documentQueries.GetGeometry(element, view)
           ?? throw new InvalidOperationException($"Element {element.Id} has no geometry.");
}