namespace Revit.Linter.CollisionDiagnostics.Abstractions.Infrasructure.Services;

internal interface IGetElementBoundingBoxService
{
    BoundingBoxXYZ Execute(Element element, View? view);

#if BEFORE2024
    BoundingBoxXYZ Execute(int elementId, GeometryElement geometryElement);
#else
    BoundingBoxXYZ Execute(long elementId, GeometryElement geometryElement);
#endif
}
