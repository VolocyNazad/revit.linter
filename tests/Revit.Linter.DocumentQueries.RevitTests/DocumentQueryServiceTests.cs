using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.DocumentQueries.Abstractions.Models;
using Revit.Linter.DocumentQueries.Abstractions.Services;
using Revit.Linter.Testing;
using TUnit.Core.Executors;

namespace Revit.Linter.DocumentQueries.RevitTests;

public sealed class DocumentQueryServiceTests : RevitApiTest
{
    private Document? _document;
    private Document? _otherDocument;
    private Level? _level;
    private Wall? _wall;
    private IDocumentQueryService? _queries;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateModel()
    {
        _document = Application.NewProjectDocument(UnitSystem.Metric);
        _otherDocument = Application.NewProjectDocument(UnitSystem.Metric);

        using Transaction transaction = new(_document, "Seed document query tests");
        transaction.Start();
        _level = Level.Create(_document, 0);
        _wall = Wall.Create(
            _document,
            Line.CreateBound(new XYZ(0, 0, 0), new XYZ(10, 0, 0)),
            _level.Id,
            false);
        transaction.Commit();

        // Created after the model is seeded: the test cache never invalidates on transactions.
        _queries = TestDocumentQueries.Create();
    }

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseModel()
    {
        _document?.Close(false);
        _otherDocument?.Close(false);
    }

    [Test]
    public async Task Equal_queries_share_one_cache_entry()
    {
        int factoryCalls = 0;

        object Factory()
        {
            factoryCalls++;
            return new object();
        }

        object first = _queries!.GetOrCreate<object>(DocumentQueryKey.Create(_document!, "test:query"), Factory);
        object second = _queries.GetOrCreate<object>(DocumentQueryKey.Create(_document!, "test:query"), Factory);
        bool sameItem = ReferenceEquals(first, second);

        await Assert.That(sameItem).IsTrue();
        await Assert.That(factoryCalls).IsEqualTo(1);
    }

    [Test]
    public async Task Statistics_count_hits_and_misses_per_query_and_reset_when_taken()
    {
        DocumentQueryKey key = DocumentQueryKey.Create(_document!, "test:query");
        _queries!.TakeStatistics();

        _queries.GetOrCreate<object>(key, () => new object());
        _queries.GetOrCreate<object>(key, () => new object());
        _queries.GetOrCreate<object>(key, () => new object());

        DocumentQueryStatistics statistics = _queries.TakeStatistics().Single(item => item.Query == "test:query");
        int remainingCount = _queries.TakeStatistics().Count(item => item.Query == "test:query");

        await Assert.That(statistics.Query).IsEqualTo("test:query");
        await Assert.That(statistics.Misses).IsEqualTo(1);
        await Assert.That(statistics.Hits).IsEqualTo(2);
        await Assert.That(remainingCount).IsEqualTo(0);
    }

    [Test]
    public async Task Queries_differing_by_name_argument_or_element_do_not_share_an_entry()
    {
        DocumentQueryKey baseKey = DocumentQueryKey.Create(_document!, "test:query");
        bool nameDiffers = baseKey != DocumentQueryKey.Create(_document!, "test:other-query");
        bool argumentDiffers =
            DocumentQueryKey.Create(_document!, "test:query", argument: "rule-a")
            != DocumentQueryKey.Create(_document!, "test:query", argument: "rule-b");
        bool elementDiffers =
            DocumentQueryKey.Create(_document!, "test:query", elementId: _wall!.Id)
            != DocumentQueryKey.Create(_document!, "test:query", elementId: _level!.Id);
        bool equalKeysMatch =
            DocumentQueryKey.Create(_document!, "test:query", elementId: _wall.Id, argument: "rule-a")
            == DocumentQueryKey.Create(_document!, "test:query", elementId: _wall.Id, argument: "rule-a");

        await Assert.That(nameDiffers).IsTrue();
        await Assert.That(argumentDiffers).IsTrue();
        await Assert.That(elementDiffers).IsTrue();
        await Assert.That(equalKeysMatch).IsTrue();
    }

    [Test]
    public async Task Queries_against_different_documents_do_not_share_an_entry()
    {
        bool keysDiffer =
            DocumentQueryKey.Create(_document!, "test:query")
            != DocumentQueryKey.Create(_otherDocument!, "test:query");
        ElementId levelId = _level!.Id;

        ElementId[] levelIds = [.. _queries!.GetElementsOfClass<Level>(_document!).Select(level => level.Id)];
        ElementId[] otherWallIds =
            [.. _queries.GetElementsOfClass<Wall>(_otherDocument!).Select(wall => wall.Id)];

        await Assert.That(keysDiffer).IsTrue();
        await Assert.That(levelIds).Contains(levelId);
        await Assert.That(otherWallIds).Count().IsEqualTo(0);
    }

    [Test]
    public async Task View_scoped_query_does_not_share_an_entry_with_the_document_query()
    {
        View view = new FilteredElementCollector(_document!)
            .OfClass(typeof(View))
            .Cast<View>()
            .First();

        bool keysDiffer =
            DocumentQueryKey.Create(_document!, "test:query")
            != DocumentQueryKey.Create(_document!, "test:query", view);
        bool sameViewMatches =
            DocumentQueryKey.Create(_document!, "test:query", view)
            == DocumentQueryKey.Create(_document!, "test:query", view);

        await Assert.That(keysDiffer).IsTrue();
        await Assert.That(sameViewMatches).IsTrue();
    }

    [Test]
    public async Task Document_elements_are_collected_once()
    {
        IReadOnlyList<Element> first = _queries!.GetElements(_document!);
        IReadOnlyList<Element> second = _queries.GetElements(_document!);
        bool sameList = ReferenceEquals(first, second);
        ElementId wallId = _wall!.Id;
        ElementId[] elementIds = [.. first.Select(element => element.Id)];

        await Assert.That(sameList).IsTrue();
        await Assert.That(elementIds).Contains(wallId);
    }

    [Test]
    public async Task Class_queries_are_cached_per_class()
    {
        IReadOnlyList<Wall> walls = _queries!.GetElementsOfClass<Wall>(_document!);
        IReadOnlyList<Level> levels = _queries.GetElementsOfClass<Level>(_document!);
        bool sameWallList = ReferenceEquals(walls, _queries.GetElementsOfClass<Wall>(_document!));
        ElementId wallId = _wall!.Id;
        ElementId levelId = _level!.Id;
        ElementId[] wallIds = [.. walls.Select(wall => wall.Id)];
        ElementId[] levelIds = [.. levels.Select(level => level.Id)];

        await Assert.That(sameWallList).IsTrue();
        await Assert.That(wallIds).Contains(wallId);
        await Assert.That(levelIds).Contains(levelId);
    }

    [Test]
    public async Task Element_types_are_collected_once()
    {
        IReadOnlyList<Element> first = _queries!.GetElementTypes(_document!);
        bool sameList = ReferenceEquals(first, _queries.GetElementTypes(_document!));
        bool onlyTypes = first.All(element => element is ElementType);
        ElementId wallTypeId = _wall!.GetTypeId();
        ElementId[] typeIds = [.. first.Select(element => element.Id)];

        await Assert.That(sameList).IsTrue();
        await Assert.That(onlyTypes).IsTrue();
        await Assert.That(typeIds).Contains(wallTypeId);
    }

    [Test]
    public async Task Element_geometry_is_computed_once_per_element()
    {
        GeometryElement? first = _queries!.GetGeometry(_wall!);
        GeometryElement? second = _queries.GetGeometry(_wall!);
        bool hasGeometry = first is not null;
        bool sameGeometry = ReferenceEquals(first, second);

        await Assert.That(hasGeometry).IsTrue();
        await Assert.That(sameGeometry).IsTrue();
    }
}