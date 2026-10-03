namespace Revit.Linter.CollisionDiagnostics.Abstractions.Infrastructure.Services;

internal interface IGetElementGeometryService
{
    IReadOnlyCollection<Solid> Execute(Element element, View? view);
}