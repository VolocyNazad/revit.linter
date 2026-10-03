namespace Revit.Linter.CollisionDiagnostics.Abstractions.Infrastructure.Services;

internal interface IGetElementBoundingBoxService
{
    BoundingBoxXYZ Execute(Element element, View? view);
}