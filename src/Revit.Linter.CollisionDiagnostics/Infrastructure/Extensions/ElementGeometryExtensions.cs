using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace Revit.Linter.CollisionDiagnostics.Infrastructure.Extensions;

internal static class ElementGeometryExtensions
{
    private const double Epsilon = 1e-6;

    public static IReadOnlyCollection<Solid> GetSolids(this GeometryElement geometryElement)
    {
        List<Solid>? list = null;

        foreach (GeometryObject geometryObject in geometryElement)
            CollectSolids(geometryObject, ref list);

        return list is not null ? list : Array.Empty<Solid>();
    }

    public static IReadOnlyCollection<Solid> GetSolids(this Element element, Options options)
    {
        List<Solid>? list = null;

        var geometryElement = element.get_Geometry(options);
        if (geometryElement is null)
            return Array.Empty<Solid>();

        foreach (GeometryObject geometryObject in geometryElement)
            CollectSolids(geometryObject, ref list);

        return list is not null ? list : Array.Empty<Solid>();
    }

    private static void CollectSolids(GeometryObject geometryObject, ref List<Solid>? list)
    {
        if (geometryObject is Solid { Volume: > Epsilon } solid)
        {
            (list ??= []).Add(solid);
        }
        else if (geometryObject is GeometryInstance geometryInstance)
        {
            foreach (GeometryObject nested in geometryInstance.GetInstanceGeometry())
                CollectSolids(nested, ref list);
        }
    }
}