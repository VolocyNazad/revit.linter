using Revit.Linter.CollisionDiagnostics.Abstractions.Infrastructure.Services;
using Revit.Linter.CollisionDiagnostics.Infrastructure.Extensions;
using Revit.Linter.DocumentQueries.Abstractions.Models;
using Revit.Linter.DocumentQueries.Abstractions.Services;

namespace Revit.Linter.CollisionDiagnostics.Infrastructure.Services;

// Derived from the shared cached geometry, so solids never trigger a second geometry calculation.
internal sealed class GetElementGeometryService(IDocumentQueryService documentQueries)
    : IGetElementGeometryService
{
    public IReadOnlyCollection<Solid> Execute(Element element, View? view)
        => documentQueries.GetOrCreate(
            DocumentQueryKey.Create(element.Document, CollisionQueryNames.Solids, view, element.Id),
            () => documentQueries.GetRequiredGeometry(element, view).GetSolids());
}