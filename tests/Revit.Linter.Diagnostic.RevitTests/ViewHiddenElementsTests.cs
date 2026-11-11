using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.ElementDiagnostics.Diagnostics.ViewHasHiddenElements;
using Revit.Linter.ElementDiagnostics.Infrastructure.Extensions;
using Revit.Linter.Testing;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class ViewHiddenElementsTests : RevitApiTest
{
    // Millimetres to Revit internal feet.
    private const double MmInFeet = 1.0 / 304.8;

    private Document? _document;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() => _document = Application.NewProjectDocument(UnitSystem.Metric);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _document?.Close(false);

    [Test]
    public async Task View_with_hidden_element_is_reported()
    {
        Document document = _document!;
        Level level = GetLevel(document);
        ViewPlan view = CreatePlan(document, level, "Hidden element check");
        CreateWall(document, level, 0);
        ElementId[] wallIds = GetWallIds(document, 1);
        Hide(document, view, wallIds);
        var diagnostic = new ViewHasHiddenElementsDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, view);

        Dictionary<string, object> arguments = feedback.AdditionalMessageArguments!;
        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That((int)arguments["hidden.count"]).IsEqualTo(1);
        ElementId[] messageIds = (ElementId[])arguments["hidden.elementIds"];
        ElementId[] dependencyIds = feedback.AdditionalTargetDependencies
            .OfType<Element>()
            .Select(element => element.Id)
            .ToArray();
        await Assert.That(messageIds.Single().IdValue()).IsEqualTo(wallIds.Single().IdValue());
        await Assert.That(dependencyIds.Single().IdValue()).IsEqualTo(wallIds.Single().IdValue());
        await Assert.That((string)arguments["hidden.elementNames"])
            .IsEqualTo(document.GetElement(wallIds.Single()).Name);
    }

    [Test]
    public async Task Hidden_elements_are_listed_in_identifier_order()
    {
        Document document = _document!;
        Level level = GetLevel(document);
        ViewPlan view = CreatePlan(document, level, "Hidden element order");
        CreateWall(document, level, 0);
        CreateWall(document, level, 1000);
        ElementId[] wallIds = GetWallIds(document, 2);
        Hide(document, view, wallIds);
        long[] expectedIds = wallIds.Select(id => id.IdValue()).OrderBy(value => value).ToArray();
        var diagnostic = new ViewHasHiddenElementsDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, view);

        Dictionary<string, object> arguments = feedback.AdditionalMessageArguments!;
        long[] messageIds = ((ElementId[])arguments["hidden.elementIds"])
            .Select(id => id.IdValue())
            .ToArray();
        long[] dependencyIds = feedback.AdditionalTargetDependencies
            .OfType<Element>()
            .Select(element => element.Id.IdValue())
            .ToArray();
        await Assert.That(messageIds.SequenceEqual(expectedIds)).IsTrue();
        await Assert.That(dependencyIds.SequenceEqual(expectedIds)).IsTrue();
        string names = (string)arguments["hidden.elementNames"];
        await Assert.That(names.Split([", "], StringSplitOptions.None).Length).IsEqualTo(2);
    }

    [Test]
    public async Task View_without_hidden_elements_is_valid()
    {
        Document document = _document!;
        Level level = GetLevel(document);
        ViewPlan view = CreatePlan(document, level, "Visible element check");
        CreateWall(document, level, 0);
        var diagnostic = new ViewHasHiddenElementsDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, view);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Element_hidden_in_another_view_does_not_report_the_view()
    {
        Document document = _document!;
        Level level = GetLevel(document);
        ViewPlan hiddenView = CreatePlan(document, level, "Hidden in this view");
        ViewPlan visibleView = CreatePlan(document, level, "Hidden elsewhere");
        CreateWall(document, level, 0);
        Hide(document, hiddenView, GetWallIds(document, 1));
        var diagnostic = new ViewHasHiddenElementsDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback hiddenFeedback = diagnostic.Execute(document, null, hiddenView);
        DiagnosticFeedback visibleFeedback = diagnostic.Execute(document, null, visibleView);

        await Assert.That(hiddenFeedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That(visibleFeedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Category_hiding_is_not_reported()
    {
        // The check covers the element-level Hide in View state only: a hidden category is a
        // different visibility mechanism and the light bulb state the API exposes through
        // Element.IsHidden does not include it.
        Document document = _document!;
        Level level = GetLevel(document);
        ViewPlan view = CreatePlan(document, level, "Hidden category check");
        Wall wall = CreateWall(document, level, 0);
        using (Transaction transaction = new(document, "Hide wall category"))
        {
            transaction.Start();
            view.SetCategoryHidden(wall.Category.Id, true);
            transaction.Commit();
        }
        var diagnostic = new ViewHasHiddenElementsDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, view);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Only_graphical_views_are_checked()
    {
        Document document = _document!;
        Level level = GetLevel(document);
        ViewPlan plan = CreatePlan(document, level, "Graphical view check");
        ViewSchedule schedule;
        using (Transaction transaction = new(document, "Create test schedule"))
        {
            transaction.Start();
            schedule = ViewSchedule.CreateSchedule(document, new ElementId(BuiltInCategory.OST_Walls));
            transaction.Commit();
        }
        ViewSheet sheet;
        using (Transaction transaction = new(document, "Create test sheet"))
        {
            transaction.Start();
            sheet = ViewSheet.Create(document, ElementId.InvalidElementId);
            transaction.Commit();
        }
        View template;
        using (Transaction transaction = new(document, "Create view template"))
        {
            transaction.Start();
            template = plan.CreateViewTemplate();
            transaction.Commit();
        }
        var filter = new ViewHasHiddenElementsDiagnosticFilter();

        await Assert.That(filter.IsRelevantFor(document, plan)).IsTrue();
        await Assert.That(filter.IsRelevantFor(document, schedule)).IsFalse();
        await Assert.That(filter.IsRelevantFor(document, sheet)).IsFalse();
        await Assert.That(filter.IsRelevantFor(document, template)).IsFalse();
    }

    [Test]
    public async Task Check_runs_in_projects_only()
    {
        var filter = new ViewHasHiddenElementsDiagnosticDocumentFilter();

        await Assert.That(filter.IsRelevantFor(_document!)).IsTrue();

        Document familyDocument = FamilyTestDocuments.OpenModelFamilyTemplate(Application);
        try
        {
            await Assert.That(filter.IsRelevantFor(familyDocument)).IsFalse();
        }
        finally
        {
            familyDocument.Close(false);
        }
    }

    [Test]
    public async Task Fix_shows_every_hidden_element_of_the_view()
    {
        Document document = _document!;
        Level level = GetLevel(document);
        ViewPlan view = CreatePlan(document, level, "Fix hidden elements");
        CreateWall(document, level, 0);
        CreateWall(document, level, 1000);
        ElementId[] wallIds = GetWallIds(document, 2);
        Hide(document, view, wallIds);
        var diagnostic = new ViewHasHiddenElementsDiagnostic(TestDocumentQueries.Create());
        await Assert.That(diagnostic.Execute(document, null, view).Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        var fix = new ShowAllHiddenViewElements(TestDocumentQueries.Create());

        bool applied;
        using (Transaction transaction = new(document, "Show hidden elements"))
        {
            transaction.Start();
            applied = fix.Execute(view);
            transaction.Commit();
        }

        await Assert.That(applied).IsTrue();
        foreach (ElementId wallId in wallIds)
        {
            Element wall = document.GetElement(wallId);
            await Assert.That(wall.IsHidden(view)).IsFalse();
        }

        var afterFix = new ViewHasHiddenElementsDiagnostic(TestDocumentQueries.Create());
        await Assert.That(afterFix.Execute(document, null, view).Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Fix_reports_not_applied_when_nothing_is_hidden()
    {
        Document document = _document!;
        Level level = GetLevel(document);
        ViewPlan view = CreatePlan(document, level, "Fix clean view");
        CreateWall(document, level, 0);
        var fix = new ShowAllHiddenViewElements(TestDocumentQueries.Create());

        bool applied;
        using (Transaction transaction = new(document, "Show hidden elements"))
        {
            transaction.Start();
            applied = fix.Execute(view);
            transaction.Commit();
        }

        await Assert.That(applied).IsFalse();
    }

    private static Level GetLevel(Document document) =>
        new FilteredElementCollector(document).OfClass(typeof(Level)).Cast<Level>().First();

    private static ViewPlan CreatePlan(Document document, Level level, string name)
    {
        ViewFamilyType viewFamilyType = new FilteredElementCollector(document)
            .OfClass(typeof(ViewFamilyType))
            .Cast<ViewFamilyType>()
            .First(type => type.ViewFamily == ViewFamily.FloorPlan);
        using Transaction transaction = new(document, "Create test plan");
        transaction.Start();
        ViewPlan view = ViewPlan.Create(document, viewFamilyType.Id, level.Id);
        view.Name = name;
        transaction.Commit();
        return view;
    }

    private static Wall CreateWall(Document document, Level level, double offsetMillimeters)
    {
        double offset = offsetMillimeters * MmInFeet;
        using Transaction transaction = new(document, "Create test wall");
        transaction.Start();
        Wall wall = Wall.Create(
            document, Line.CreateBound(new XYZ(0, offset, 0), new XYZ(10, offset, 0)), level.Id, false);
        transaction.Commit();
        return wall;
    }

    private static ElementId[] GetWallIds(Document document, int count) => new FilteredElementCollector(document)
        .OfClass(typeof(Wall))
        .Take(count)
        .Select(wall => wall.Id)
        .ToArray();

    private static void Hide(Document document, View view, ICollection<ElementId> elementIds)
    {
        using Transaction transaction = new(document, "Hide test elements");
        transaction.Start();
        view.HideElements(elementIds);
        transaction.Commit();
    }
}
