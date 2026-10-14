using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.ElementDiagnostics.Diagnostics.SheetEmpty;
using Revit.Linter.ElementDiagnostics.Diagnostics.SheetWithMultipleTitleBlocks;
using Revit.Linter.ElementDiagnostics.Diagnostics.SheetWithoutTitleBlock;
using Revit.Linter.ElementDiagnostics.Diagnostics.ViewUnused;
using Revit.Linter.ElementDiagnostics.Diagnostics.ViewOnSheetWithoutTemplate;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class ViewSheetDiagnosticsTests : RevitApiTest
{
    private Document? _document;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() => _document = Application.NewProjectDocument(UnitSystem.Metric);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _document?.Close(false);

    [Test]
    public async Task Empty_sheet_is_reported()
    {
        Document document = _document!;
        ViewSheet sheet = CreateSheet(document);
        var diagnostic = new SheetEmptyDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, sheet);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Sheet_with_schedule_is_valid()
    {
        Document document = _document!;
        ViewSheet sheet;
        using (Transaction transaction = new(document, "Place test schedule"))
        {
            transaction.Start();
            sheet = ViewSheet.Create(document, ElementId.InvalidElementId);
            ViewSchedule schedule = ViewSchedule.CreateSchedule(
                document, new ElementId(BuiltInCategory.OST_Walls));
            ScheduleSheetInstance.Create(document, sheet.Id, schedule.Id, XYZ.Zero);
            transaction.Commit();
        }
        var diagnostic = new SheetEmptyDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, sheet);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Sheet_without_title_block_is_reported()
    {
        Document document = _document!;
        ViewSheet sheet = CreateSheet(document);
        var diagnostic = new SheetWithoutTitleBlockDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, sheet);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Sheet_with_two_title_blocks_is_reported()
    {
        Document document = _document!;
        ViewSheet sheet = CreateSheet(document);
        FamilyTestDocuments.PlaceTitleBlocks(document, sheet, 2);
        var diagnostic = new SheetWithMultipleTitleBlocksDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, sheet);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Sheet_with_single_title_block_is_valid()
    {
        Document document = _document!;
        ViewSheet sheet = CreateSheet(document);
        FamilyTestDocuments.PlaceTitleBlocks(document, sheet, 1);
        var diagnostic = new SheetWithMultipleTitleBlocksDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, sheet);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Only_sheets_are_checked_by_sheet_filters()
    {
        Document document = _document!;
        ViewSheet sheet = CreateSheet(document);
        ViewSchedule schedule = CreateSchedule(document);
        var emptyFilter = new SheetEmptyDiagnosticFilter();
        var missingFilter = new SheetWithoutTitleBlockDiagnosticFilter();
        var multipleFilter = new SheetWithMultipleTitleBlocksDiagnosticFilter();

        await Assert.That(emptyFilter.IsRelevantFor(document, sheet)).IsTrue();
        await Assert.That(missingFilter.IsRelevantFor(document, sheet)).IsTrue();
        await Assert.That(multipleFilter.IsRelevantFor(document, sheet)).IsTrue();
        await Assert.That(emptyFilter.IsRelevantFor(document, schedule)).IsFalse();
        await Assert.That(missingFilter.IsRelevantFor(document, schedule)).IsFalse();
        await Assert.That(multipleFilter.IsRelevantFor(document, schedule)).IsFalse();
    }

    [Test]
    public async Task Only_views_are_checked_by_view_filters()
    {
        Document document = _document!;
        ViewSheet sheet = CreateSheet(document);
        ViewSchedule schedule = CreateSchedule(document);
        var unplacedFilter = new ViewUnplacedDiagnosticFilter();
        var templateFilter = new ViewOnSheetWithoutTemplateDiagnosticFilter();

        await Assert.That(unplacedFilter.IsRelevantFor(document, schedule)).IsTrue();
        await Assert.That(unplacedFilter.IsRelevantFor(document, sheet)).IsFalse();
        // An unplaced schedule still passes: every schedule reports a dependent viewport
        // id even with no viewports in the document, so the placed-gate is vacuous for
        // schedules. This matches Execute, which reports unplaced views without a template.
        await Assert.That(templateFilter.IsRelevantFor(document, schedule)).IsTrue();
        await Assert.That(templateFilter.IsRelevantFor(document, sheet)).IsFalse();
    }

    [Test]
    public async Task Placed_view_is_checked_by_template_filter()
    {
        Document document = _document!;
        ViewSheet sheet = CreateSheet(document);
        ViewSchedule schedule = CreateSchedule(document);
        using (Transaction transaction = new(document, "Place test schedule"))
        {
            transaction.Start();
            ScheduleSheetInstance.Create(document, sheet.Id, schedule.Id, XYZ.Zero);
            transaction.Commit();
        }
        var filter = new ViewOnSheetWithoutTemplateDiagnosticFilter();

        await Assert.That(filter.IsRelevantFor(document, schedule)).IsTrue();
    }

    [Test]
    public async Task Sheet_checks_run_in_projects_only()
    {
        var emptyFilter = new SheetEmptyDiagnosticDocumentFilter();
        var missingFilter = new SheetWithoutTitleBlockDiagnosticDocumentFilter();
        var multipleFilter = new SheetWithMultipleTitleBlocksDiagnosticDocumentFilter();
        var unplacedFilter = new ViewUnplacedDiagnosticDocumentFilter();
        var templateFilter = new ViewOnSheetWithoutTemplateDiagnosticDocumentFilter();
        await Assert.That(emptyFilter.IsRelevantFor(_document!)).IsTrue();
        await Assert.That(missingFilter.IsRelevantFor(_document!)).IsTrue();
        await Assert.That(multipleFilter.IsRelevantFor(_document!)).IsTrue();
        await Assert.That(unplacedFilter.IsRelevantFor(_document!)).IsTrue();
        await Assert.That(templateFilter.IsRelevantFor(_document!)).IsTrue();

        Document familyDocument = FamilyTestDocuments.OpenModelFamilyTemplate(Application);
        try
        {
            await Assert.That(emptyFilter.IsRelevantFor(familyDocument)).IsFalse();
            await Assert.That(missingFilter.IsRelevantFor(familyDocument)).IsFalse();
            await Assert.That(multipleFilter.IsRelevantFor(familyDocument)).IsFalse();
            await Assert.That(unplacedFilter.IsRelevantFor(familyDocument)).IsFalse();
            await Assert.That(templateFilter.IsRelevantFor(familyDocument)).IsFalse();
        }
        finally
        {
            familyDocument.Close(false);
        }
    }

    [Test]
    public async Task Unplaced_schedule_is_reported()
    {
        Document document = _document!;
        ViewSchedule schedule;
        using (Transaction transaction = new(document, "Create test schedule"))
        {
            transaction.Start();
            schedule = ViewSchedule.CreateSchedule(document, new ElementId(BuiltInCategory.OST_Walls));
            transaction.Commit();
        }
        var diagnostic = new ViewUnplacedDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, schedule);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Schedule_without_template_is_reported()
    {
        Document document = _document!;
        ViewSchedule schedule;
        using (Transaction transaction = new(document, "Create test schedule"))
        {
            transaction.Start();
            schedule = ViewSchedule.CreateSchedule(document, new ElementId(BuiltInCategory.OST_Walls));
            transaction.Commit();
        }
        var diagnostic = new ViewOnSheetWithoutTemplateDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, schedule);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    private static ViewSheet CreateSheet(Document document)
    {
        using Transaction transaction = new(document, "Create test sheet");
        transaction.Start();
        ViewSheet sheet = ViewSheet.Create(document, ElementId.InvalidElementId);
        transaction.Commit();
        return sheet;
    }

    private static ViewSchedule CreateSchedule(Document document)
    {
        using Transaction transaction = new(document, "Create test schedule");
        transaction.Start();
        ViewSchedule schedule = ViewSchedule.CreateSchedule(document, new ElementId(BuiltInCategory.OST_Walls));
        transaction.Commit();
        return schedule;
    }
}
