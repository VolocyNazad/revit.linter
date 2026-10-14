namespace Revit.Linter.CollisionDiagnostics.Infrastructure.Extensions;

internal static class BoundingBoxTransformExtensions
{
    // Moves a bounding box into another coordinate space through an arbitrary transform: every
    // corner is transformed and the result is the axis-aligned box around them. Transforming only
    // the minimum and maximum corners is not enough once the transform rotates.
    public static BoundingBoxXYZ Transformed(this BoundingBoxXYZ box, Transform transform)
    {
        XYZ min = box.Min;
        XYZ max = box.Max;
        double minX = double.PositiveInfinity, minY = double.PositiveInfinity, minZ = double.PositiveInfinity;
        double maxX = double.NegativeInfinity, maxY = double.NegativeInfinity, maxZ = double.NegativeInfinity;

        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                for (int z = 0; z < 2; z++)
                {
                    XYZ corner = transform.OfPoint(new XYZ(
                        x == 0 ? min.X : max.X,
                        y == 0 ? min.Y : max.Y,
                        z == 0 ? min.Z : max.Z));
                    minX = Math.Min(minX, corner.X);
                    minY = Math.Min(minY, corner.Y);
                    minZ = Math.Min(minZ, corner.Z);
                    maxX = Math.Max(maxX, corner.X);
                    maxY = Math.Max(maxY, corner.Y);
                    maxZ = Math.Max(maxZ, corner.Z);
                }
            }
        }

        return new BoundingBoxXYZ { Min = new XYZ(minX, minY, minZ), Max = new XYZ(maxX, maxY, maxZ) };
    }
}
