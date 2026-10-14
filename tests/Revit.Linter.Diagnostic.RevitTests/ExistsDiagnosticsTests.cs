using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.ElementDiagnostics.Diagnostics.DetailCurveExists;
using Revit.Linter.ElementDiagnostics.Diagnostics.ModelCurveExists;
using Revit.Linter.ElementDiagnostics.Diagnostics.TextNoteExists;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class ExistsDiagnosticsTests : RevitApiTest
{
    private Document? _document;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() => _document = Application.NewProjectDocument(UnitSystem.Metric);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _document?.Close(false);

    [Test]
    public async Task Model_curve_is_reported()
    {
        Document document = _document!;
        ModelCurve curve;
        using (Transaction transaction = new(document, "Create test model curve"))
        {
            transaction.Start();
            SketchPlane plane = SketchPlane.Create(
                document, Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero));
            curve = document.Create.NewModelCurve(Line.CreateBound(XYZ.Zero, new XYZ(1, 0, 0)), plane);
            transaction.Commit();
        }
        var diagnostic = new ModelCurveExistsDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, curve);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Detail_curve_on_sheet_is_reported()
    {
        Document document = _document!;
        DetailCurve curve;
        using (Transaction transaction = new(document, "Create test detail curve"))
        {
            transaction.Start();
            ViewSheet sheet = ViewSheet.Create(document, ElementId.InvalidElementId);
            curve = document.Create.NewDetailCurve(
                sheet, Line.CreateBound(XYZ.Zero, new XYZ(1, 0, 0)));
            transaction.Commit();
        }
        var diagnostic = new DetailCurveExistsDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, curve);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Text_note_in_family_is_reported()
    {
        // An empty project document has no text note types, so TextNote.Create has
        // nothing to use: the note is created in a family document, whose template
        // brings its own text types and a plan view.
        Document familyDocument = FamilyTestDocuments.OpenModelFamilyTemplate(Application);
        try
        {
            ViewPlan view = new FilteredElementCollector(familyDocument)
                .OfClass(typeof(ViewPlan))
                .Cast<ViewPlan>()
                .First();
            TextNoteType type = new FilteredElementCollector(familyDocument)
                .OfClass(typeof(TextNoteType))
                .Cast<TextNoteType>()
                .First();
            TextNote note;
            using (Transaction transaction = new(familyDocument, "Create test text note"))
            {
                transaction.Start();
                note = TextNote.Create(
                    familyDocument, view.Id, XYZ.Zero, "Linter", new TextNoteOptions(type.Id));
                transaction.Commit();
            }
            var diagnostic = new TextNoteExistsDiagnostic();

            DiagnosticFeedback feedback = diagnostic.Execute(familyDocument, null, note);

            await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        }
        finally
        {
            familyDocument.Close(false);
        }
    }

    [Test]
    public async Task Detail_curve_is_checked_by_filter()
    {
        Document document = _document!;
        DetailCurve curve;
        using (Transaction transaction = new(document, "Create test detail curve"))
        {
            transaction.Start();
            ViewSheet sheet = ViewSheet.Create(document, ElementId.InvalidElementId);
            curve = document.Create.NewDetailCurve(
                sheet, Line.CreateBound(XYZ.Zero, new XYZ(1, 0, 0)));
            transaction.Commit();
        }
        var filter = new DetailCurveExistsDiagnosticFilter();

        await Assert.That(filter.IsRelevantFor(document, curve)).IsTrue();
    }

    [Test]
    public async Task Model_curve_is_ignored_by_detail_curve_filter()
    {
        Document document = _document!;
        ModelCurve curve;
        using (Transaction transaction = new(document, "Create test model curve"))
        {
            transaction.Start();
            SketchPlane plane = SketchPlane.Create(
                document, Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero));
            curve = document.Create.NewModelCurve(Line.CreateBound(XYZ.Zero, new XYZ(1, 0, 0)), plane);
            transaction.Commit();
        }
        var filter = new DetailCurveExistsDiagnosticFilter();

        await Assert.That(filter.IsRelevantFor(document, curve)).IsFalse();
    }

    [Test]
    public async Task Detail_check_runs_in_projects_only()
    {
        var filter = new DetailCurveExistsDiagnosticDocumentFilter();
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
    public async Task Model_curve_is_checked_by_filter()
    {
        Document document = _document!;
        ModelCurve curve;
        using (Transaction transaction = new(document, "Create test model curve"))
        {
            transaction.Start();
            SketchPlane plane = SketchPlane.Create(
                document, Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero));
            curve = document.Create.NewModelCurve(Line.CreateBound(XYZ.Zero, new XYZ(1, 0, 0)), plane);
            transaction.Commit();
        }
        var filter = new ModelCurveExistsDiagnosticFilter();

        await Assert.That(filter.IsRelevantFor(document, curve)).IsTrue();
    }

    [Test]
    public async Task Detail_curve_is_ignored_by_model_curve_filter()
    {
        Document document = _document!;
        DetailCurve curve;
        using (Transaction transaction = new(document, "Create test detail curve"))
        {
            transaction.Start();
            ViewSheet sheet = ViewSheet.Create(document, ElementId.InvalidElementId);
            curve = document.Create.NewDetailCurve(
                sheet, Line.CreateBound(XYZ.Zero, new XYZ(1, 0, 0)));
            transaction.Commit();
        }
        var filter = new ModelCurveExistsDiagnosticFilter();

        await Assert.That(filter.IsRelevantFor(document, curve)).IsFalse();
    }

    [Test]
    public async Task Model_check_runs_in_projects_only()
    {
        var filter = new ModelCurveExistsDiagnosticDocumentFilter();
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
    public async Task Text_note_is_checked_by_filter()
    {
        // Same reason as Text_note_in_family_is_reported: an empty project has
        // no text types, so the note is built in a family document.
        Document familyDocument = FamilyTestDocuments.OpenModelFamilyTemplate(Application);
        try
        {
            ViewPlan view = new FilteredElementCollector(familyDocument)
                .OfClass(typeof(ViewPlan))
                .Cast<ViewPlan>()
                .First();
            TextNoteType type = new FilteredElementCollector(familyDocument)
                .OfClass(typeof(TextNoteType))
                .Cast<TextNoteType>()
                .First();
            TextNote note;
            using (Transaction transaction = new(familyDocument, "Create test text note"))
            {
                transaction.Start();
                note = TextNote.Create(
                    familyDocument, view.Id, XYZ.Zero, "Linter", new TextNoteOptions(type.Id));
                transaction.Commit();
            }
            var filter = new TextNoteExistsDiagnosticFilter();

            await Assert.That(filter.IsRelevantFor(familyDocument, note)).IsTrue();
            await Assert.That(filter.IsRelevantFor(familyDocument, view)).IsFalse();
        }
        finally
        {
            familyDocument.Close(false);
        }
    }

    [Test]
    public async Task Text_note_check_runs_in_projects_only()
    {
        var filter = new TextNoteExistsDiagnosticDocumentFilter();
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
}
