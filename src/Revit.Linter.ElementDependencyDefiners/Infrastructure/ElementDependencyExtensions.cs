using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Mechanical;
using Toolkit.Revit.Extensions;

namespace Revit.Linter.ElementDependencyDefiners.Infrastructure;

internal static class ElementDependencyExtensions
{
    private static readonly ElementFilter RoomFilter = new RoomFilter();
    private static readonly ElementFilter SpaceFilter = new SpaceFilter();
    private static readonly ElementFilter ScopeBoxFilter =
        new ElementCategoryFilter(BuiltInCategory.OST_VolumeOfInterest);
    private static readonly ElementFilter FamilyInstanceFilter =
        new ElementClassFilter(typeof(FamilyInstance));
    private static readonly ElementFilter ElementIsNotElementTypeFilter =
        new ElementIsElementTypeFilter(inverted: true);
    private static readonly Options GeometryOptions = new();

    public static BuiltInCategory GetBuiltInCategory(this Category category)
    {
        long value = category.Id.Value();
        return Enum.IsDefined(typeof(BuiltInCategory), value)
            ? (BuiltInCategory)value
            : BuiltInCategory.INVALID;
    }

    public static Element? GetElementHere(this Element element, ElementId id)
        => element.Document.GetElement(id);

    public static ElementType? FindElementType(this Element element)
    {
        ElementId typeId = element.GetTypeId();
        return typeId == ElementId.InvalidElementId
            ? null
            : element.Document.GetElement(typeId) as ElementType;
    }

    public static IEnumerable<Element> FindInstances(this ElementType elementType)
    {
        Document document = elementType.Document;
        foreach (ElementId elementId in elementType.GetDependentElements(ElementIsNotElementTypeFilter))
        {
            Element instance = document.GetElement(elementId);
            if (instance.GetTypeId() == elementType.Id)
                yield return instance;
        }
    }

    public static Group? FindGroup(this Element element)
        => element.GroupId == ElementId.InvalidElementId
            ? null
            : element.Document.GetElement(element.GroupId) as Group;

    public static IEnumerable<Element> FindMembers(this Group group, bool recursive = true)
    {
        foreach (ElementId memberId in group.GetMemberIds())
        {
            Element member = group.Document.GetElement(memberId);
            yield return member;

            if (recursive && member is Group nestedGroup)
                foreach (Element nestedMember in nestedGroup.FindMembers())
                    yield return nestedMember;
        }
    }

    public static IEnumerable<Element> FindInserted(this HostObject hostObject)
    {
        foreach (ElementId insertId in hostObject.FindInserts(false, false, false, false))
            if (hostObject.Document.GetElement(insertId) is { } insert)
                yield return insert;
    }

    public static IEnumerable<Element> FindInserted(this FamilyInstance host)
    {
        foreach (FamilyInstance instance in Collect(
                     host.Document,
                     "family-instances",
                     () => new FilteredElementCollector(host.Document)
                         .WherePasses(FamilyInstanceFilter)).OfType<FamilyInstance>())
            if (instance.Host?.Id == host.Id)
                yield return instance;
    }

    public static FamilyInstance? GetSuperPuperComponent(this FamilyInstance instance)
    {
        FamilyInstance? result = null;
        for (FamilyInstance? current = instance.SuperComponent as FamilyInstance;
             current is not null;
             current = current.SuperComponent as FamilyInstance)
            result = current;
        return result;
    }

    public static IEnumerable<FamilyInstance> GetSubComponents(
        this FamilyInstance instance,
        bool recursive = true)
    {
        foreach (ElementId id in instance.GetSubComponentIds())
        {
            if (instance.Document.GetElement(id) is not FamilyInstance child)
                continue;

            yield return child;
            if (recursive)
                foreach (FamilyInstance descendant in child.GetSubComponents())
                    yield return descendant;
        }
    }

    public static IEnumerable<Element> FindConnected(this Element element)
    {
        HashSet<ElementId> visited = [];
        ConnectorManager? manager = element switch
        {
            MEPCurve curve => curve.ConnectorManager,
            FamilyInstance instance => instance.MEPModel?.ConnectorManager,
            _ => null,
        };
        if (manager is null)
            yield break;

        foreach (Connector connector in manager.Connectors)
        {
            if (!connector.IsConnected)
                continue;

            foreach (Connector reference in connector.AllRefs)
                if (reference.Owner.Id != element.Id && visited.Add(reference.Owner.Id))
                    yield return reference.Owner;
        }
    }

    public static Element? FindRoom(this Element element)
        => element.FindRooms().FirstOrDefault();

    public static IEnumerable<Element> FindRooms(this Element element)
    {
        if (element.Location is not LocationPoint location)
            yield break;

        foreach (Room room in Collect(
                     element.Document,
                     "rooms",
                     () => new FilteredElementCollector(element.Document)
                         .WherePasses(RoomFilter)).OfType<Room>())
            if (room.IsPointInRoom(location.Point))
                yield return room;
    }

    public static Element? FindSpace(this Element element)
        => element.FindSpaces().FirstOrDefault();

    public static IEnumerable<Element> FindSpaces(this Element element)
    {
        if (element.Location is not LocationPoint location)
            yield break;

        foreach (Space space in Collect(
                     element.Document,
                     "spaces",
                     () => new FilteredElementCollector(element.Document)
                         .WherePasses(SpaceFilter)).OfType<Space>())
            if (space.IsPointInSpace(location.Point))
                yield return space;
    }

    public static IEnumerable<Element> FindPlaced<TElement>(this Room room)
        where TElement : Element
        => FindPlaced<TElement>(room.Document, room.IsPointInRoom);

    public static IEnumerable<Element> FindPlaced<TElement>(this Space space)
        where TElement : Element
        => FindPlaced<TElement>(space.Document, space.IsPointInSpace);

    private static IEnumerable<Element> FindPlaced<TElement>(Document document, Func<XYZ, bool> contains)
        where TElement : Element
    {
        foreach (Element element in Collect(
                     document,
                     $"class:{typeof(TElement).FullName}",
                     () => new FilteredElementCollector(document)
                         .OfClass(typeof(TElement))))
            if (element.Location is LocationPoint location && contains(location.Point))
                yield return element;
    }

    public static Element? FindScopeBox(this Element element)
        => element.FindScopeBoxes().FirstOrDefault();

    public static IEnumerable<Element> FindScopeBoxes(this Element element)
    {
        foreach (Element scopeBox in Collect(
                     element.Document,
                     "scope-boxes",
                     () => new FilteredElementCollector(element.Document)
                         .WherePasses(ScopeBoxFilter)))
            if (scopeBox.IsElementInScopeBox(element.Id))
                yield return scopeBox;
    }

    public static IEnumerable<Element> FindPlacedInsideScopeBox(this Element scopeBox)
    {
        Outline? outline = GetScopeBoxOutline(scopeBox);
        return outline is null
            ? []
            : Collect(
                scopeBox.Document,
                $"scope-box:{scopeBox.Id}:elements",
                () => new FilteredElementCollector(scopeBox.Document)
                    .WhereElementIsNotElementType()
                    .WherePasses(new BoundingBoxIntersectsFilter(outline)));
    }

    private static bool IsElementInScopeBox(this Element scopeBox, ElementId elementId)
        => scopeBox.FindPlacedInsideScopeBox().Any(element => element.Id == elementId);

    private static Outline? GetScopeBoxOutline(Element scopeBox)
    {
        List<XYZ> points = [];
        foreach (GeometryObject geometryObject in scopeBox.get_Geometry(GeometryOptions))
            CollectCurvePoints(geometryObject, points);

        if (points.Count == 0)
            return null;

        return new Outline(
            new XYZ(points.Min(point => point.X), points.Min(point => point.Y), points.Min(point => point.Z)),
            new XYZ(points.Max(point => point.X), points.Max(point => point.Y), points.Max(point => point.Z)));
    }

    private static void CollectCurvePoints(GeometryObject geometryObject, ICollection<XYZ> points)
    {
        if (geometryObject is Curve curve)
        {
            points.Add(curve.GetEndPoint(0));
            points.Add(curve.GetEndPoint(1));
        }
        else if (geometryObject is GeometryInstance instance)
        {
            foreach (GeometryObject nestedObject in instance.GetInstanceGeometry())
                CollectCurvePoints(nestedObject, points);
        }
    }

    private static IEnumerable<Element> Collect(
        Document document,
        string collectorKey,
        Func<FilteredElementCollector> collectorFactory)
        => DocumentElementCollectorCache.GetOrCreate(
            document,
            collectorKey,
            () => collectorFactory().ToElementIds());

    public static IEnumerable<Element> FindExternalInsulations(this Element element)
        => FindInsulations(element, includeExternal: true, includeInternal: false);

    public static IEnumerable<Element> FindInternalInsulations(this Element element)
        => FindInsulations(element, includeExternal: false, includeInternal: true);

    public static IEnumerable<Element> FindInsulations(this Element element)
        => FindInsulations(element, includeExternal: true, includeInternal: true);

    private static IEnumerable<Element> FindInsulations(
        Element element,
        bool includeExternal,
        bool includeInternal)
    {
        if (element is ElementType || element.Category is null)
            yield break;

        BuiltInCategory category = element.Category.GetBuiltInCategory();
        bool isDuct = InsulationUtils.DuctCategoryElementsWithInsulation.Contains(category);
        bool isPipe = InsulationUtils.PipeCategoryElementsWithInsulation.Contains(category);
        if (!isDuct && !isPipe)
            yield break;

        if (includeExternal)
            foreach (ElementId id in InsulationLiningBase.GetInsulationIds(element.Document, element.Id))
                yield return element.Document.GetElement(id);

        if (includeInternal && isDuct)
            foreach (ElementId id in InsulationLiningBase.GetLiningIds(element.Document, element.Id))
                yield return element.Document.GetElement(id);
    }
}

internal static class InsulationUtils
{
    public static readonly HashSet<BuiltInCategory> DuctCategoryElementsWithInsulation =
        new HashSet<BuiltInCategory>
        {
            BuiltInCategory.OST_DuctCurves,
            BuiltInCategory.OST_DuctFitting,
            BuiltInCategory.OST_FlexDuctCurves,
            BuiltInCategory.OST_DuctAccessory,
        };

    public static readonly HashSet<BuiltInCategory> PipeCategoryElementsWithInsulation =
        new HashSet<BuiltInCategory>
        {
            BuiltInCategory.OST_PipeCurves,
            BuiltInCategory.OST_PipeFitting,
            BuiltInCategory.OST_FlexPipeCurves,
            BuiltInCategory.OST_PipeAccessory,
        };
}
