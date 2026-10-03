using Revit.Linter.CollisionDiagnostics.Abstractions.Infrastructure.Services;
using Revit.Linter.CollisionDiagnostics.Infrastructure.Extensions;
using Revit.Linter.DocumentQueries.Abstractions.Models;
using Revit.Linter.DocumentQueries.Abstractions.Services;

namespace Revit.Linter.CollisionDiagnostics.Infrastructure.Services;

// Derived from the shared cached geometry, so a bounding box never triggers a second geometry calculation.
internal sealed class GetElementBoundingBoxService(IDocumentQueryService documentQueries)
    : IGetElementBoundingBoxService
{
    public BoundingBoxXYZ Execute(Element element, View? view)
        => documentQueries.GetOrCreate(
            DocumentQueryKey.Create(element.Document, CollisionQueryNames.BoundingBox, view, element.Id),
            () => documentQueries.GetRequiredGeometry(element, view).GetBoundingBox());
}