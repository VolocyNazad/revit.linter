namespace Revit.Linter.CollisionDiagnostics.Infrasructure.Extensions;

internal static class ElementGeometryExtensions
{
    private const double EPSILON = 1e-6;

    public static ICollection<Solid> GetSolids(this GeometryElement geometryElement)
    {
        ICollection<Solid> collection = []; // todo нужно не создавать список, если это не требуется

        foreach (var geometryObject in geometryElement)
            AddSolids(geometryObject, collection);

        return collection;
    }
    public static ICollection<Solid> GetSolids(this Element element, Options opt)
    {
        ICollection<Solid> collection = []; // todo нужно не создавать список, если это не требуется

        var geometryElement = element.get_Geometry(opt);
        if (geometryElement is not null)
        {
            foreach (var geometryObject in geometryElement)
                AddSolids(geometryObject, collection);
        }

        return collection;
    }
    private static void AddSolids(GeometryObject geometryObject, ICollection<Solid> collection)
    {
        if (geometryObject is Solid { Volume: > EPSILON } solid)
            collection.Add(solid);
        else if (geometryObject is GeometryInstance geometryInstance)
            geometryInstance.ExplodeGeometryInstance(collection);
    }

    private static void ExplodeGeometryInstance(this GeometryInstance geometry, ICollection<Solid> collection)
    {
        foreach (GeometryObject geometryObject in geometry.GetInstanceGeometry())
            AddSolids(geometryObject, collection);
    }
}
