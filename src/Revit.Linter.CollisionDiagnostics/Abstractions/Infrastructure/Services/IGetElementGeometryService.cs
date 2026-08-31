namespace Revit.Linter.CollisionDiagnostics.Abstractions.Infrastructure.Services;

internal interface IGetElementGeometryService
{
    IReadOnlyCollection<Solid> Execute(Element element, View? view);

#if BEFORE2024
    IReadOnlyCollection<Solid> Execute(int elementId, GeometryElement geometryElement);
#else
    IReadOnlyCollection<Solid> Execute(long elementId, GeometryElement geometryElement);
#endif
}