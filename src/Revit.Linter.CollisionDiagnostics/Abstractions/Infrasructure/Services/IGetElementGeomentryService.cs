namespace Revit.Linter.CollisionDiagnostics.Abstractions.Infrasructure.Services;

internal interface IGetElementGeomentryService
{
    ICollection<Solid> Execute(Element element, View? view);

#if BEFORE2024
    ICollection<Solid> Execute(int elementId, GeometryElement geometryElement);
#else
    ICollection<Solid> Execute(long elementId, GeometryElement geometryElement);
#endif
}