using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Toolkit.Revit.Extensions;
using TUnit.Core.Executors;

namespace Revit.Linter.ProjectParameterManaging.Tests;

public sealed class RevitExtensionTests : RevitApiTest
{
    [Test]
    public async Task Overlaps_returns_true_for_intersecting_boxes()
    {
        BoundingBoxXYZ first = CreateBoundingBox(new XYZ(0, 0, 0), new XYZ(2, 2, 2));
        BoundingBoxXYZ second = CreateBoundingBox(new XYZ(1, 1, 1), new XYZ(3, 3, 3));

        await Assert.That(first.Overlaps(second)).IsTrue();
    }

    [Test]
    public async Task Overlaps_returns_false_for_separated_boxes()
    {
        BoundingBoxXYZ first = CreateBoundingBox(new XYZ(0, 0, 0), new XYZ(1, 1, 1));
        BoundingBoxXYZ second = CreateBoundingBox(new XYZ(2, 2, 2), new XYZ(3, 3, 3));

        await Assert.That(first.Overlaps(second)).IsFalse();
    }

    [Test]
    public async Task Overlaps_applies_bounding_box_transform()
    {
        BoundingBoxXYZ first = CreateBoundingBox(new XYZ(0, 0, 0), new XYZ(1, 1, 1));
        BoundingBoxXYZ second = CreateBoundingBox(new XYZ(0, 0, 0), new XYZ(1, 1, 1));
        second.Transform = Transform.CreateTranslation(new XYZ(2, 0, 0));

        await Assert.That(first.Overlaps(second)).IsFalse();
    }

    [Test]
    public async Task IsCategory_compares_category_identifier()
    {
        var categoryId = new ElementId(BuiltInCategory.OST_Walls);

        await Assert.That(categoryId.IsCategory(BuiltInCategory.OST_Walls)).IsTrue();
        await Assert.That(categoryId.IsCategory(BuiltInCategory.OST_Doors)).IsFalse();
    }

    [Test]
    [HookExecutor<RevitThreadExecutor>]
    public async Task WhereElementIs_applies_instance_and_type_filters()
    {
        Document document = Application.NewProjectDocument(UnitSystem.Metric);
        try
        {
            Element[] levels = new FilteredElementCollector(document)
                .WhereElementIs<Level>()
                .ToElements()
                .ToArray();
            Element[] wallTypes = new FilteredElementCollector(document)
                .WhereElementIs<WallType>()
                .ToElements()
                .ToArray();

            await Assert.That(levels).IsNotEmpty();
            await Assert.That(wallTypes).IsNotEmpty();
            await Assert.That(levels.All(element => element is Level)).IsTrue();
            await Assert.That(wallTypes.All(element => element is WallType)).IsTrue();
        }
        finally
        {
            document.Close(false);
        }
    }

    private static BoundingBoxXYZ CreateBoundingBox(XYZ min, XYZ max) => new()
    {
        Min = min,
        Max = max,
    };
}
