using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.ElementDiagnostics.Diagnostics.LinkOnSharedWorkset;
using Revit.Linter.ElementDiagnostics.Infrastructure.Extensions;
using Revit.Linter.Testing;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

/// <summary>
/// Covers the link-on-shared-workset diagnostic (SHRD0042): a link that shares its workset with
/// model elements is a finding that names those elements, a link alone on its workset is not, and
/// the check stays out of family documents and out of documents without worksharing. Links are
/// built from a project document saved to a temporary file, because the Revit API can link only an
/// existing model.
/// </summary>
public sealed class LinkOnSharedWorksetTests : RevitApiTest
{
    private const string SharedWorksetName = "Model";
    private const string LinkWorksetName = "Links";

    private Document? _document;
    private string? _linkPath;
    private WorksetId? _linkWorksetId;
    private RevitLinkInstance? _linkOnSharedWorkset;
    private RevitLinkInstance? _linkOnLinkWorkset;
    private Wall? _wallOnSharedWorkset;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument()
    {
        _document = Application.NewProjectDocument(UnitSystem.Metric);
        if (!_document.CanEnableWorksharing())
            throw new InvalidOperationException(
                "The new project document does not allow worksharing to be enabled.");
        _document.EnableWorksharing("Shared Levels and Grids", SharedWorksetName);
        if (!_document.IsWorkshared)
            throw new InvalidOperationException(
                "Worksharing was not enabled in the new project document.");

        using (Transaction transaction = new(_document, "Create link workset"))
        {
            transaction.Start();
            _linkWorksetId = Workset.Create(_document, LinkWorksetName).Id;
            transaction.Commit();
        }

        _linkPath = Path.Combine(Path.GetTempPath(), $"LinkSharedWorkset{Guid.NewGuid():N}.rvt");
        Document linkDocument = Application.NewProjectDocument(UnitSystem.Metric);
        using (Transaction linkTransaction = new(linkDocument, "Seed linked test document"))
        {
            linkTransaction.Start();
            Level.Create(linkDocument, 0.0);
            linkTransaction.Commit();
        }
        linkDocument.SaveAs(_linkPath);
        linkDocument.Close(false);

        using (Transaction transaction = new(_document, "Create test link type"))
        {
            transaction.Start();
            LinkLoadResult linkResult = RevitLinkType.Create(
                _document,
                ModelPathUtils.ConvertUserVisiblePathToModelPath(_linkPath),
                new RevitLinkOptions(false));
            if (linkResult.ElementId == ElementId.InvalidElementId)
                throw new InvalidOperationException(
                    $"The link workset test document could not be linked: {linkResult.LoadResult}.");
            transaction.Commit();
        }

        WorksetId sharedWorksetId = GetWorksetId(_document, SharedWorksetName);
        _wallOnSharedWorkset = CreateWall(_document, sharedWorksetId);
        _linkOnSharedWorkset = CreateLink(_document, sharedWorksetId);
        _linkOnLinkWorkset = CreateLink(_document, _linkWorksetId);
    }

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument()
    {
        _document?.Close(false);
        TryDelete(_linkPath);
    }

    [Test]
    public async Task Link_sharing_a_workset_with_a_wall_is_reported()
    {
        Document document = _document!;
        var diagnostic = new LinkOnSharedWorksetDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, _linkOnSharedWorkset!).Single();

        Dictionary<string, object> arguments = feedback.AdditionalMessageArguments!;
        ElementId[] elementIds = (ElementId[])arguments["other.elementIds"];
        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That((string)arguments["workset.name"]).IsEqualTo(SharedWorksetName);
        await Assert.That((int)arguments["other.count"]).IsEqualTo(elementIds.Length);
        await Assert.That(elementIds.Select(id => id.IdValue())).Contains(_wallOnSharedWorkset!.Id.IdValue());
        await Assert.That(feedback.AdditionalTargetDependencies
            .OfType<Element>()
            .Select(element => element.Id.IdValue())).Contains(_wallOnSharedWorkset.Id.IdValue());
    }

    [Test]
    public async Task Link_alone_on_its_workset_is_valid()
    {
        Document document = _document!;
        var diagnostic = new LinkOnSharedWorksetDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, _linkOnLinkWorkset!).Single();

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Two_links_on_one_workset_are_valid()
    {
        Document document = _document!;
        RevitLinkInstance secondLink = CreateLink(document, _linkWorksetId!);
        var diagnostic = new LinkOnSharedWorksetDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback firstFeedback = diagnostic.Execute(document, null, _linkOnLinkWorkset!).Single();
        DiagnosticFeedback secondFeedback = diagnostic.Execute(document, null, secondLink).Single();

        await Assert.That(firstFeedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
        await Assert.That(secondFeedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Elements_are_listed_in_identifier_order()
    {
        Document document = _document!;
        Wall secondWall = CreateWall(document, GetWorksetId(document, SharedWorksetName), 5.0);
        var diagnostic = new LinkOnSharedWorksetDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, _linkOnSharedWorkset!).Single();

        ElementId[] elementIds = (ElementId[])feedback.AdditionalMessageArguments!["other.elementIds"];
        long[] actualIds = elementIds.Select(id => id.IdValue()).ToArray();
        long[] expectedIds = actualIds.OrderBy(value => value).ToArray();
        string[] elementNames = (string[])feedback.AdditionalMessageArguments["other.elementNames"];
        await Assert.That(actualIds.SequenceEqual(expectedIds)).IsTrue();
        await Assert.That(actualIds.Length).IsEqualTo(elementNames.Length);
        await Assert.That(actualIds).Contains(secondWall.Id.IdValue());
    }

    [Test]
    public async Task Creating_a_view_keeps_the_link_alone_on_its_workset_valid()
    {
        Document document = _document!;
        ViewPlan view = CreatePlan(document, _linkWorksetId!, "Link workset view");
        var diagnostic = new LinkOnSharedWorksetDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, _linkOnLinkWorkset!).Single();

        // A view belongs to its own view workset, so creating it while the link workset is active
        // leaves the link alone on that workset and keeps the link clean.
        await Assert.That(view.WorksetId.IntegerValue).IsNotEqualTo(_linkWorksetId!.IntegerValue);
        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Non_link_elements_are_not_checked()
    {
        Document document = _document!;
        Level level = GetLevel(document);
        var filter = new LinkOnSharedWorksetDiagnosticFilter();
        var diagnostic = new LinkOnSharedWorksetDiagnostic(TestDocumentQueries.Create());

        await Assert.That(filter.IsRelevantFor(document, _linkOnSharedWorkset!)).IsTrue();
        await Assert.That(filter.IsRelevantFor(document, _wallOnSharedWorkset!)).IsFalse();
        await Assert.That(filter.IsRelevantFor(document, level)).IsFalse();
        await Assert.That(diagnostic.Execute(document, null, _wallOnSharedWorkset!)[0].Verdict)
            .IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Unloaded_link_keeps_its_workset_finding()
    {
        Document document = _document!;
        RevitLinkType linkType = new FilteredElementCollector(document)
            .OfClass(typeof(RevitLinkType))
            .Cast<RevitLinkType>()
            .First();
        linkType.Unload(null);
        var diagnostic = new LinkOnSharedWorksetDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, _linkOnSharedWorkset!).Single();

        await Assert.That(_linkOnSharedWorkset!.GetLinkDocument()).IsNull();
        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Check_runs_in_workshared_projects_only()
    {
        var documentFilter = new LinkOnSharedWorksetDiagnosticDocumentFilter();
        Document projectDocument = _document!;
        Document plainProjectDocument = Application.NewProjectDocument(UnitSystem.Metric);
        Document familyDocument = FamilyTestDocuments.OpenModelFamilyTemplate(Application);
        try
        {
            await Assert.That(documentFilter.IsRelevantFor(projectDocument)).IsTrue();
            await Assert.That(documentFilter.IsRelevantFor(plainProjectDocument)).IsFalse();
            await Assert.That(documentFilter.IsRelevantFor(familyDocument)).IsFalse();
        }
        finally
        {
            plainProjectDocument.Close(false);
            familyDocument.Close(false);
        }
    }

    private static Level GetLevel(Document document) => new FilteredElementCollector(document)
        .OfClass(typeof(Level))
        .Cast<Level>()
        .First();

    private static WorksetId GetWorksetId(Document document, string worksetName)
        => new FilteredWorksetCollector(document)
            .OfKind(WorksetKind.UserWorkset)
            .First(workset => string.Equals(workset.Name, worksetName, StringComparison.Ordinal))
            .Id;

    private static RevitLinkInstance CreateLink(Document document, WorksetId worksetId)
    {
        RevitLinkType linkType = new FilteredElementCollector(document)
            .OfClass(typeof(RevitLinkType))
            .Cast<RevitLinkType>()
            .FirstOrDefault()
            ?? throw new InvalidOperationException(
                "The test document has no Revit link type after the link document was linked.");
        document.GetWorksetTable().SetActiveWorksetId(worksetId);
        using Transaction transaction = new(document, "Create test link instance");
        transaction.Start();
        RevitLinkInstance instance = RevitLinkInstance.Create(document, linkType.Id);
        transaction.Commit();
        return instance;
    }

    private static Wall CreateWall(Document document, WorksetId worksetId, double y = 0.0)
    {
        Level level = GetLevel(document);
        document.GetWorksetTable().SetActiveWorksetId(worksetId);
        using Transaction transaction = new(document, "Create test wall");
        transaction.Start();
        Wall wall = Wall.Create(
            document, Line.CreateBound(new XYZ(0, y, 0), new XYZ(10, y, 0)), level.Id, false);
        transaction.Commit();
        return wall;
    }

    private static ViewPlan CreatePlan(Document document, WorksetId worksetId, string name)
    {
        document.GetWorksetTable().SetActiveWorksetId(worksetId);
        ViewFamilyType viewFamilyType = new FilteredElementCollector(document)
            .OfClass(typeof(ViewFamilyType))
            .Cast<ViewFamilyType>()
            .First(type => type.ViewFamily == ViewFamily.FloorPlan);
        Level level = GetLevel(document);
        using Transaction transaction = new(document, "Create test plan");
        transaction.Start();
        ViewPlan view = ViewPlan.Create(document, viewFamilyType.Id, level.Id);
        view.Name = name;
        transaction.Commit();
        return view;
    }

    private static void TryDelete(string? path)
    {
        if (path is null || !File.Exists(path)) return;
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // The test model is still locked by Revit: the temporary folder cleans it up later.
        }
    }
}
