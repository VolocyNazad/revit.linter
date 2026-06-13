using Revit.Linter.ElementAccentor.Infrastructure.Implementations;

namespace Revit.Linter.ElementAccentor.Infrastructure.Extensions;

public static class View3DExtensions
{
    extension(View3D view)
    {
        public bool SetSectionBoxBy(
            IEnumerable<Element> elements, double heightOffset = 0, double widthOffset = 0, double lengthOffset = 0)
        {
            Document doc = view.Document;

            IEnumerable<BoundingBoxXYZ> boundingBoxes = elements
                .Select(element => element.get_BoundingBox(view)).Where(i => i != null).ToList();
            IEnumerable<XYZ> splitedPoints = boundingBoxes.SelectMany(i => new[] { i.Min, i.Max }).ToList();

            if (!splitedPoints.Any()) return false;

            DirectShape directShape = DirectShape.CreateElement(
                view.Document,
                Category.GetCategory(view.Document, BuiltInCategory.OST_GenericModel).Id
            );
            directShape.SetShape(splitedPoints.Select(Point.Create).Cast<GeometryObject>().ToList());

            BoundingBoxXYZ boundingBox = new()
            {
                Min = new XYZ(
                    splitedPoints.Min(i => i.X),
                    splitedPoints.Min(i => i.Y),
                    splitedPoints.Min(i => i.Z)),
                Max = new XYZ(
                    splitedPoints.Max(i => i.X),
                    splitedPoints.Max(i => i.Y),
                    splitedPoints.Max(i => i.Z))
            };

            SizableBoundingBoxXYZ sizableBoundingBox = new SizableBoundingBoxXYZ()
                .NewAlign()
                .NewSize(boundingBox.Min, boundingBox.Max);

            FormatOptions formatOptions = doc.GetUnits().GetFormatOptions(SpecTypeId.Length);
            ForgeTypeId unitTypeId = formatOptions.GetUnitTypeId();

            sizableBoundingBox.Height += UnitUtils.ConvertToInternalUnits(heightOffset, unitTypeId);
            sizableBoundingBox.Width += UnitUtils.ConvertToInternalUnits(widthOffset, unitTypeId);
            sizableBoundingBox.Length += UnitUtils.ConvertToInternalUnits(lengthOffset, unitTypeId);

            view.SetSectionBox(sizableBoundingBox);

            doc.Delete(directShape.Id);

            return true;
        }
    }
}
