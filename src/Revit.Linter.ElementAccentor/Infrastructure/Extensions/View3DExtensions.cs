namespace Revit.Linter.ElementAccentor.Infrastructure.Extensions;

internal static class View3DExtensions
{
    extension(View3D view)
    {
        public bool SetSectionBoxBy(
            IEnumerable<Element> elements, double heightOffset = 0, double widthOffset = 0, double lengthOffset = 0)
        {
            List<XYZ> points = elements
                .Select(element => element.get_BoundingBox(view))
                .Where(boundingBox => boundingBox is not null)
                .SelectMany(GetModelCorners)
                .ToList();

            if (points.Count == 0) return false;

            double halfHeightOffset = UnitUtils.ConvertToInternalUnits(heightOffset, UnitTypeId.Millimeters) / 2;
            double halfWidthOffset = UnitUtils.ConvertToInternalUnits(widthOffset, UnitTypeId.Millimeters) / 2;
            double halfLengthOffset = UnitUtils.ConvertToInternalUnits(lengthOffset, UnitTypeId.Millimeters) / 2;

            BoundingBoxXYZ sectionBox = new()
            {
                Min = new XYZ(
                    points.Min(point => point.X) - halfLengthOffset,
                    points.Min(point => point.Y) - halfWidthOffset,
                    points.Min(point => point.Z) - halfHeightOffset),
                Max = new XYZ(
                    points.Max(point => point.X) + halfLengthOffset,
                    points.Max(point => point.Y) + halfWidthOffset,
                    points.Max(point => point.Z) + halfHeightOffset)
            };

            view.SetSectionBox(sectionBox);

            return true;
        }

        private static IEnumerable<XYZ> GetModelCorners(BoundingBoxXYZ boundingBox)
        {
            XYZ min = boundingBox.Min;
            XYZ max = boundingBox.Max;
            Transform transform = boundingBox.Transform;

            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int z = 0; z < 2; z++)
                    {
                        yield return transform.OfPoint(new XYZ(
                            x == 0 ? min.X : max.X,
                            y == 0 ? min.Y : max.Y,
                            z == 0 ? min.Z : max.Z));
                    }
                }
            }
        }
    }
}
