using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;
using Revit.TransactionMemoryCache.Abstractions.Services;
using TUnit.Core.Executors;

namespace Revit.Linter.ElementDependencyDefiners.RevitTests;

public sealed class DependencyDefinerTests : RevitApiTest
{
    private Document? _document;
    private Level? _level;
    private Wall? _firstWall;
    private Wall? _secondWall;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateModel()
    {
        _document = Application.NewProjectDocument(UnitSystem.Metric);
        DocumentElementCollectorCache.Initialize(new TestTransactionMemoryCache());

        using Transaction transaction = new(_document, "Seed dependency definer tests");
        transaction.Start();
        _level = Level.Create(_document, 0);
        _firstWall = CreateWall(0);
        _secondWall = CreateWall(5);
        transaction.Commit();
    }

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseModel()
    {
        if (_document is null)
            return;

        _document.Close(false);
    }

    [Test]
    public async Task Collector_cache_reuses_transaction_cache_results()
    {
        int factoryCalls = 0;
        const string collectorKey = "test:walls";

        IEnumerable<ElementId> Factory()
        {
            factoryCalls++;
            return [_firstWall!.Id, _secondWall!.Id];
        }

        ElementId[] first = [.. DocumentElementCollectorCache
            .GetOrCreate(_document!, collectorKey, Factory)
            .Select(element => element.Id)];
        ElementId[] second = [.. DocumentElementCollectorCache
            .GetOrCreate(_document!, collectorKey, Factory)
            .Select(element => element.Id)];

        await Assert.That(first).IsEquivalentTo(second);
        await Assert.That(factoryCalls).IsEqualTo(1);
    }

    [Test]
    public async Task Internal_definer_returns_the_source_element()
    {
        var definer = new InternalDependencyDefiner();

        Element[] all = [.. definer.All(_level!)];
        ElementId sourceId = _level!.Id;
        ElementId resultId = all[0].Id;
        ElementId firstId = definer.FirstOrDefault(_level)!.Id;

        await Assert.That(all).Count().IsEqualTo(1);
        await Assert.That(resultId).IsEqualTo(sourceId);
        await Assert.That(firstId).IsEqualTo(sourceId);
    }

    [Test]
    public async Task Type_and_instances_definers_are_inverse_relations()
    {
        var typeDefiner = new TypeDependencyDefiner();
        ElementType type = (ElementType)typeDefiner.FirstOrDefault(_firstWall!)!;
        ElementId typeId = type.Id;
        ElementId expectedTypeId = _firstWall!.GetTypeId();
        ElementId firstWallId = _firstWall.Id;
        ElementId secondWallId = _secondWall!.Id;
        ElementId[] instances = [.. typeDefiner.Inversed.All(type).Select(element => element.Id)];

        await Assert.That(typeId).IsEqualTo(expectedTypeId);
        await Assert.That(instances).Contains(firstWallId);
        await Assert.That(instances).Contains(secondWallId);
    }

    [Test]
    public async Task Group_and_members_definers_are_inverse_relations()
    {
        Group expectedGroup = CreateGroup();
        var groupDefiner = new GeneralGroupDependencyDefiner();

        Element? group = groupDefiner.FirstOrDefault(_firstWall!);
        ElementId[] members = [.. groupDefiner.Inversed.All(group!).Select(element => element.Id)];
        ElementId groupId = group!.Id;
        ElementId expectedGroupId = expectedGroup.Id;
        ElementId firstWallId = _firstWall!.Id;
        ElementId secondWallId = _secondWall!.Id;

        await Assert.That(groupId).IsEqualTo(expectedGroupId);
        await Assert.That(members).Contains(firstWallId);
        await Assert.That(members).Contains(secondWallId);
    }

    [Test]
    public async Task Group_type_and_instance_members_definers_are_inverse_relations()
    {
        Group expectedGroup = CreateGroup();
        var groupTypeDefiner = new GeneralGroupTypeDependencyDefiner();

        Element? groupType = groupTypeDefiner.FirstOrDefault(_firstWall!);
        ElementId[] members = [.. groupTypeDefiner.Inversed.All(groupType!).Select(element => element.Id)];
        ElementId groupTypeId = groupType!.Id;
        ElementId expectedGroupTypeId = expectedGroup.GetTypeId();
        ElementId firstWallId = _firstWall!.Id;
        ElementId secondWallId = _secondWall!.Id;

        await Assert.That(groupTypeId).IsEqualTo(expectedGroupTypeId);
        await Assert.That(members).Contains(firstWallId);
        await Assert.That(members).Contains(secondWallId);
    }

    [Test]
    public async Task Element_filter_definer_collects_matching_elements()
    {
        var definer = new ElementFilterDependencyDefiner(
            new ElementCategoryFilter(BuiltInCategory.OST_Walls));

        ElementId[] elements = [.. definer.All(_level!).Select(element => element.Id)];
        ElementId firstWallId = _firstWall!.Id;
        ElementId secondWallId = _secondWall!.Id;
        bool hasFirst = definer.FirstOrDefault(_level!) is not null;

        await Assert.That(elements).Contains(firstWallId);
        await Assert.That(elements).Contains(secondWallId);
        await Assert.That(hasFirst).IsTrue();
    }

    [Test]
    public async Task With_filter_definer_filters_the_wrapped_result()
    {
        var definer = new WithElementFilterDependencyDefiner(
            new InternalDependencyDefiner(),
            new ElementCategoryFilter(BuiltInCategory.OST_Walls));
        bool wallMatched = definer.FirstOrDefault(_firstWall!) is not null;
        bool levelMatched = definer.FirstOrDefault(_level!) is not null;

        await Assert.That(wallMatched).IsTrue();
        await Assert.That(levelMatched).IsFalse();
    }

    [Test]
    public async Task Set_combinators_apply_union_intersection_and_difference()
    {
        var walls = new FixedDependencyDefiner(_firstWall!, _secondWall!);
        var firstWall = new FixedDependencyDefiner(_firstWall!);

        ElementId[] union = [.. new UnionDependencyDefiner(walls, firstWall)
            .All(_level!).Select(element => element.Id)];
        ElementId[] intersection = [.. new IntersectDependencyDefiner(walls, firstWall)
            .All(_level!).Select(element => element.Id)];
        ElementId[] difference = [.. new ExceptDependencyDefiner(walls, firstWall)
            .All(_level!).Select(element => element.Id)];

        await Assert.That(union).Count().IsEqualTo(2);
        await Assert.That(intersection).Count().IsEqualTo(1);
        await Assert.That(intersection).Contains(_firstWall!.Id);
        await Assert.That(difference).Count().IsEqualTo(1);
        await Assert.That(difference).Contains(_secondWall!.Id);
    }

    [Test]
    public async Task Empty_definer_always_returns_no_dependency()
    {
        var definer = new EmptyDependencyDefiner();

        await Assert.That(definer.All(_level!)).IsEmpty();
        await Assert.That(definer.FirstOrDefault(_level!)).IsNull();
        await Assert.That(definer.Inversed).IsNull();
    }

    [Test]
    public async Task Registry_contains_every_concrete_definer()
    {
        Type[] expected = [..
            typeof(IElementsDependencyDefiner).Assembly.GetTypes()
                .Where(type => typeof(IElementsDependencyDefiner).IsAssignableFrom(type))
                .Where(type => !type.IsInterface && !type.IsAbstract)
                .OrderBy(type => type.Name)];

        IList<Type> actual = ElementsDependencyDefinerExtensions.GetElementsDependencyDefinerTypes();

        await Assert.That(actual).IsEquivalentTo(expected);
    }

    [Test]
    public async Task Every_parameterless_definer_accepts_an_unrelated_element()
    {
        IList<Type> types = ElementsDependencyDefinerExtensions
            .GetWithEmptyConstructorElementsDependencyDefinerTypes();

        foreach (Type type in types)
        {
            var definer = (IElementsDependencyDefiner)Activator.CreateInstance(type)!;
            _ = definer.All(_level!).ToArray();
            _ = definer.FirstOrDefault(_level!);
        }

        await Assert.That(types).IsNotEmpty();
    }

    private Wall CreateWall(double y)
        => Wall.Create(
            _document!,
            Line.CreateBound(new XYZ(0, y, 0), new XYZ(10, y, 0)),
            _level!.Id,
            false);

    private Group CreateGroup()
    {
        using Transaction transaction = new(_document!, "Create test group");
        transaction.Start();
        Group group = _document!.Create.NewGroup([_firstWall!.Id, _secondWall!.Id]);
        transaction.Commit();

        Wall[] members = [.. group.GetMemberIds()
            .Select(_document.GetElement)
            .OfType<Wall>()
            .OrderBy(wall => ((LocationCurve)wall.Location).Curve.GetEndPoint(0).Y)];
        _firstWall = members[0];
        _secondWall = members[1];
        return group;
    }

    private sealed class FixedDependencyDefiner(params Element[] elements)
        : IElementsDependencyDefiner
    {
        public IElementsDependencyDefiner? Inversed => null;
        public IEnumerable<Element> All(Element element) => elements;
        public Element? FirstOrDefault(Element element) => elements.FirstOrDefault();
    }

    private sealed class TestTransactionMemoryCache : IRevitTransactionMemoryCache
    {
        private readonly Dictionary<object, object?> _items = [];

        public TItem? GetOrCreate<TItem>(object key, Func<TItem> factory)
        {
            if (_items.TryGetValue(key, out object? value))
                return (TItem?)value;

            TItem item = factory();
            _items[key] = item;
            return item;
        }
    }
}
