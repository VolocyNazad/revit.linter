using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.ElementDiagnostics.Diagnostics.ArrayWithFormula;
using Revit.Linter.ElementDiagnostics.Diagnostics.AssociatedArray;
using Revit.Linter.ElementDiagnostics.Diagnostics.LargeArray;
using Revit.Linter.ElementDiagnostics.Diagnostics.LargeRadialArray;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class ArrayDiagnosticsTests : RevitApiTest
{
    private Document? _document;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() => _document = Application.NewProjectDocument(UnitSystem.Metric);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _document?.Close(false);

    [Test]
    public async Task Associated_small_project_array_is_reported()
    {
        Document document = _document!;
        LinearArray array = CreateProjectLinearArray(document, 2);

        DiagnosticFeedback feedback = new AssociatedArrayDiagnostic().Execute(document, null, array);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    // Revit 2021 does not permit creating the test family geometry through the API on the opened
    // templates (the same limitation that disables the connector-cube tests), so the family-array
    // cases cannot be built there; the project arrays cover the shared behavior.
#if !IS2021
    [Test]
    public async Task Associated_family_array_is_reported()
    {
        Document familyDocument = FamilyTestDocuments.OpenModelFamilyTemplate(Application);
        try
        {
            BaseArray array = CreateFamilyLinearArray(familyDocument, 2);

            DiagnosticFeedback feedback = new AssociatedArrayDiagnostic().Execute(familyDocument, null, array);

            await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        }
        finally
        {
            familyDocument.Close(false);
        }
    }
#endif

    [Test]
    public async Task Associated_filter_only_arrays()
    {
        Document document = _document!;
        LinearArray array = CreateProjectLinearArray(document, 2);
        ElementId memberId = array.GetOriginalMemberIds().First();
        Element member = document.GetElement(memberId)
            ?? throw new InvalidOperationException("The test array member was not found.");
        var filter = new AssociatedArrayDiagnosticFilter();

        await Assert.That(filter.IsRelevantFor(document, array)).IsTrue();
        await Assert.That(filter.IsRelevantFor(document, member)).IsFalse();
    }

    [Test]
    public async Task Associated_runs_anywhere()
    {
        var filter = new AssociatedArrayDiagnosticDocumentFilter();
        await Assert.That(filter.IsRelevantFor(_document!)).IsTrue();

        Document familyDocument = FamilyTestDocuments.OpenModelFamilyTemplate(Application);
        try
        {
            await Assert.That(filter.IsRelevantFor(familyDocument)).IsTrue();
        }
        finally
        {
            familyDocument.Close(false);
        }
    }

    [Test]
    public async Task Associated_delete_removes_array()
    {
        Document document = _document!;
        LinearArray array = CreateProjectLinearArray(document, 2);
        ElementId arrayId = array.Id;
        var fix = new DeleteAssociatedArray();

        bool result;
        using (Transaction transaction = new(document, "Delete test array"))
        {
            transaction.Start();
            result = fix.Execute(array);
            transaction.Commit();
        }

        await Assert.That(fix.Identity.Code).IsEqualTo("SHRD0031");
        await Assert.That(result).IsTrue();
        await Assert.That(document.GetElement(arrayId)).IsNull();
    }

    [Test]
    public async Task Large_project_array_below_threshold_is_valid()
    {
        Document document = _document!;
        LinearArray array = CreateProjectLinearArray(document, 99);

        DiagnosticFeedback feedback = new LargeArrayDiagnostic().Execute(document, null, array);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Large_project_array_at_threshold_is_reported()
    {
        Document document = _document!;
        LinearArray array = CreateProjectLinearArray(document, 100);

        DiagnosticFeedback feedback = new LargeArrayDiagnostic().Execute(document, null, array);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

#if !IS2021
    [Test]
    public async Task Large_small_family_array_is_valid()
    {
        Document familyDocument = FamilyTestDocuments.OpenModelFamilyTemplate(Application);
        try
        {
            // Large family arrays cannot be built through the API (see the repository
            // task list): the constructible small array proves the family branch reads
            // the family threshold, and the boundary itself is covered by projects.
            BaseArray array = CreateFamilyLinearArray(familyDocument, 2);

            DiagnosticFeedback feedback = new LargeArrayDiagnostic().Execute(familyDocument, null, array);

            await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
        }
        finally
        {
            familyDocument.Close(false);
        }
    }
#endif

    [Test]
    public async Task Large_runs_anywhere()
    {
        var filter = new LargeArrayDiagnosticDocumentFilter();
        await Assert.That(filter.IsRelevantFor(_document!)).IsTrue();

        Document familyDocument = FamilyTestDocuments.OpenModelFamilyTemplate(Application);
        try
        {
            await Assert.That(filter.IsRelevantFor(familyDocument)).IsTrue();
        }
        finally
        {
            familyDocument.Close(false);
        }
    }

    [Test]
    public async Task Large_delete_removes_array()
    {
        Document document = _document!;
        LinearArray array = CreateProjectLinearArray(document, 100);
        ElementId arrayId = array.Id;
        var fix = new DeleteLargeArray();

        bool result;
        using (Transaction transaction = new(document, "Delete test array"))
        {
            transaction.Start();
            result = fix.Execute(array);
            transaction.Commit();
        }

        await Assert.That(fix.Identity.Code).IsEqualTo("SHRD0032");
        await Assert.That(result).IsTrue();
        await Assert.That(document.GetElement(arrayId)).IsNull();
    }

#if !IS2021
    [Test]
    public async Task Formula_unlabeled_family_array_is_valid()
    {
        Document familyDocument = FamilyTestDocuments.OpenModelFamilyTemplate(Application);
        try
        {
            BaseArray array = CreateFamilyLinearArray(familyDocument, 2);

            DiagnosticFeedback feedback = new ArrayWithFormulaDiagnostic().Execute(familyDocument, null, array);

            await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
        }
        finally
        {
            familyDocument.Close(false);
        }
    }
#endif

#if !IS2021
    [Test]
    public async Task Formula_labeled_family_array_is_reported()
    {
        Document familyDocument = FamilyTestDocuments.OpenModelFamilyTemplate(Application);
        try
        {
            BaseArray created = CreateFamilyLinearArray(familyDocument, 2);
            ElementId arrayId = created.Id;
            using (Transaction transaction = new(familyDocument, "Label test family array"))
            {
                transaction.Start();
                BaseArray labeled = (BaseArray)familyDocument.GetElement(arrayId)
                    ?? throw new InvalidOperationException("The test array was not found.");
                labeled.Label = AddIntegerParameter(familyDocument, "LinterCount");
                transaction.Commit();
            }
            BaseArray array = (BaseArray)familyDocument.GetElement(arrayId)
                ?? throw new InvalidOperationException("The test array was not found.");

            DiagnosticFeedback feedback = new ArrayWithFormulaDiagnostic().Execute(familyDocument, null, array);

            await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        }
        finally
        {
            familyDocument.Close(false);
        }
    }
#endif

    [Test]
    public async Task Formula_runs_anywhere()
    {
        var filter = new ArrayWithFormulaDiagnosticDocumentFilter();
        await Assert.That(filter.IsRelevantFor(_document!)).IsTrue();

        Document familyDocument = FamilyTestDocuments.OpenModelFamilyTemplate(Application);
        try
        {
            await Assert.That(filter.IsRelevantFor(familyDocument)).IsTrue();
        }
        finally
        {
            familyDocument.Close(false);
        }
    }

#if !IS2021
    [Test]
    public async Task Formula_unbind_clears_label_and_keeps_array()
    {
        Document familyDocument = FamilyTestDocuments.OpenModelFamilyTemplate(Application);
        try
        {
            BaseArray created = CreateFamilyLinearArray(familyDocument, 2);
            ElementId arrayId = created.Id;
            using (Transaction transaction = new(familyDocument, "Label test family array"))
            {
                transaction.Start();
                BaseArray labeled = (BaseArray)familyDocument.GetElement(arrayId)
                    ?? throw new InvalidOperationException("The test array was not found.");
                labeled.Label = AddIntegerParameter(familyDocument, "LinterCount");
                transaction.Commit();
            }
            var fix = new UnbindArrayWithFormula();
            BaseArray target = (BaseArray)familyDocument.GetElement(arrayId)
                ?? throw new InvalidOperationException("The test array was not found.");

            bool result;
            using (Transaction transaction = new(familyDocument, "Unbind test array"))
            {
                transaction.Start();
                result = fix.Execute(target);
                transaction.Commit();
            }

            await Assert.That(fix.Identity.Code).IsEqualTo("SHRD0033");
            await Assert.That(result).IsTrue();
            await Assert.That(((BaseArray)familyDocument.GetElement(arrayId)!).Label).IsNull();
            await Assert.That(familyDocument.GetElement(arrayId)).IsNotNull();
        }
        finally
        {
            familyDocument.Close(false);
        }
    }
#endif

    [Test]
    public async Task Formula_delete_removes_array()
    {
        Document document = _document!;
        LinearArray array = CreateProjectLinearArray(document, 2);
        ElementId arrayId = array.Id;
        var fix = new DeleteArrayWithFormula();

        bool result;
        using (Transaction transaction = new(document, "Delete test array"))
        {
            transaction.Start();
            result = fix.Execute(array);
            transaction.Commit();
        }

        await Assert.That(fix.Identity.Code).IsEqualTo("SHRD0033");
        await Assert.That(result).IsTrue();
        await Assert.That(document.GetElement(arrayId)).IsNull();
    }

    [Test]
    public async Task Formula_fixes_reject_non_array()
    {
        Document document = _document!;
        DirectShape target;
        using (Transaction transaction = new(document, "Create test element"))
        {
            transaction.Start();
            ElementId categoryId = Category.GetCategory(document, BuiltInCategory.OST_GenericModel).Id;
            target = DirectShape.CreateElement(document, categoryId);
            transaction.Commit();
        }

        bool unbind;
        bool delete;
        using (Transaction transaction = new(document, "Reject test element"))
        {
            transaction.Start();
            unbind = new UnbindArrayWithFormula().Execute(target);
            delete = new DeleteArrayWithFormula().Execute(target);
            transaction.RollBack();
        }

        await Assert.That(unbind).IsFalse();
        await Assert.That(delete).IsFalse();
    }

    [Test]
    public async Task Radial_small_array_is_valid()
    {
        Document document = _document!;
        RadialArray array = CreateProjectRadialArray(document, 3);

        DiagnosticFeedback feedback = new LargeRadialArrayDiagnostic().Execute(document, null, array);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Radial_array_below_threshold_is_valid()
    {
        Document document = _document!;
        RadialArray array = CreateProjectRadialArray(document, 49);

        DiagnosticFeedback feedback = new LargeRadialArrayDiagnostic().Execute(document, null, array);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Radial_array_at_threshold_is_reported()
    {
        Document document = _document!;
        RadialArray array = CreateProjectRadialArray(document, 50);

        DiagnosticFeedback feedback = new LargeRadialArrayDiagnostic().Execute(document, null, array);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Radial_filter_only_radials()
    {
        Document document = _document!;
        RadialArray radial = CreateProjectRadialArray(document, 3, "LinterRadialHost");
        LinearArray linear = CreateProjectLinearArray(document, 2, "LinterArrayHost");
        var filter = new LargeRadialArrayDiagnosticFilter();

        await Assert.That(filter.IsRelevantFor(document, radial)).IsTrue();
        await Assert.That(filter.IsRelevantFor(document, linear)).IsFalse();
    }

    [Test]
    public async Task Radial_runs_anywhere()
    {
        var filter = new LargeRadialArrayDiagnosticDocumentFilter();
        await Assert.That(filter.IsRelevantFor(_document!)).IsTrue();

        Document familyDocument = FamilyTestDocuments.OpenModelFamilyTemplate(Application);
        try
        {
            await Assert.That(filter.IsRelevantFor(familyDocument)).IsTrue();
        }
        finally
        {
            familyDocument.Close(false);
        }
    }

    [Test]
    public async Task Radial_delete_removes_array()
    {
        Document document = _document!;
        RadialArray array = CreateProjectRadialArray(document, 50);
        ElementId arrayId = array.Id;
        var fix = new DeleteLargeRadialArray();

        bool result;
        using (Transaction transaction = new(document, "Delete test array"))
        {
            transaction.Start();
            result = fix.Execute(array);
            transaction.Commit();
        }

        await Assert.That(fix.Identity.Code).IsEqualTo("SHRD0034");
        await Assert.That(result).IsTrue();
        await Assert.That(document.GetElement(arrayId)).IsNull();
    }

    private static LinearArray CreateProjectLinearArray(Document document, int count, string typeName = "LinterArrayHost")
    {
        // LoadFamily closes its own transactions: it must run before any transaction
        // is opened on the target document.
        _ = FamilyTestDocuments.LoadFamilyWithType(
            document.Application!, document, typeName);
        ElementId arrayId;
        using (Transaction transaction = new(document, "Create test array"))
        {
            transaction.Start();
            FamilySymbol symbol = FamilyTestDocuments.FindSymbolByTypeName(document, typeName);
            Level level = RequireLevel(document);
            FamilyInstance instance = FamilyTestDocuments.PlaceInstance(document, symbol, level, XYZ.Zero);
            document.Regenerate();
            View view = RequireView(document);
            LinearArray created = LinearArray.Create(
                document, view, instance.Id, count, new XYZ(5, 0, 0), ArrayAnchorMember.Second);
            arrayId = created.Id;
            transaction.Commit();
        }
        // The Create return must not be trusted across the commit: collect afresh.
        return (LinearArray)document.GetElement(arrayId)
            ?? throw new InvalidOperationException("The test array was not found.");
    }

    private static RadialArray CreateProjectRadialArray(Document document, int count, string typeName = "LinterArrayHost")
    {
        // LoadFamily closes its own transactions: it must run before any transaction
        // is opened on the target document.
        _ = FamilyTestDocuments.LoadFamilyWithType(
            document.Application!, document, typeName);
        ElementId arrayId;
        using (Transaction transaction = new(document, "Create test radial array"))
        {
            transaction.Start();
            FamilySymbol symbol = FamilyTestDocuments.FindSymbolByTypeName(document, typeName);
            Level level = RequireLevel(document);
            FamilyInstance instance = FamilyTestDocuments.PlaceInstance(document, symbol, level, XYZ.Zero);
            document.Regenerate();
            View view = RequireView(document);
            Line axis = Line.CreateBound(new XYZ(10, 0, 0), new XYZ(10, 0, 10));
            // Last: the angle is the total spread. With Second a full-circle multiple
            // would place the last member exactly on the first one and Create fails.
            RadialArray created = RadialArray.Create(
                document, view, instance.Id, count, axis, Math.PI, ArrayAnchorMember.Last);
            arrayId = created.Id;
            transaction.Commit();
        }
        // The Create return must not be trusted across the commit: collect afresh.
        return (RadialArray)document.GetElement(arrayId)
            ?? throw new InvalidOperationException("The test radial array was not found.");
    }

#if !IS2021
    private static BaseArray CreateFamilyLinearArray(Document familyDocument, int count)
    {
        using (Transaction transaction = new(familyDocument, "Create test family array"))
        {
            transaction.Start();
            Extrusion form = CreateFamilyForm(familyDocument);
            familyDocument.Regenerate();
            View view = RequireView(familyDocument);
            _ = LinearArray.Create(
                familyDocument, view, form.Id, count, new XYZ(6, 0, 0), ArrayAnchorMember.Second);
            transaction.Commit();
        }
        // Collect afresh after the commit instead of trusting the Create return.
        // Large family arrays cannot be built through the API (see the repository task
        // list): counts that drop the association leave plain copies here.
        BaseArray[] arrays = new FilteredElementCollector(familyDocument)
            .OfClass(typeof(BaseArray))
            .Cast<BaseArray>()
            .ToArray();
        return arrays.SingleOrDefault()
            ?? throw new InvalidOperationException(
                $"The test family array was not found. BaseArrays: {arrays.Length}, " +
                $"total elements: {new FilteredElementCollector(familyDocument).GetElementCount()}, " +
                $"warnings: {string.Join("; ", familyDocument.GetWarnings().Select(warning => warning.GetDescriptionText()))}.");
    }

    private static Extrusion CreateFamilyForm(Document familyDocument)
    {
        Plane plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero);
        SketchPlane sketch = SketchPlane.Create(familyDocument, plane);
        CurveArray profile = new();
        XYZ p0 = new(0, 0, 0);
        XYZ p1 = new(5, 0, 0);
        XYZ p2 = new(5, 3, 0);
        XYZ p3 = new(0, 3, 0);
        profile.Append(Line.CreateBound(p0, p1));
        profile.Append(Line.CreateBound(p1, p2));
        profile.Append(Line.CreateBound(p2, p3));
        profile.Append(Line.CreateBound(p3, p0));
        CurveArrArray profiles = new();
        profiles.Append(profile);
        return familyDocument.FamilyCreate.NewExtrusion(true, profiles, sketch, 2.0);
    }
#endif

#if !IS2021
    private static FamilyParameter AddIntegerParameter(Document familyDocument, string name) =>
        familyDocument.FamilyManager.AddParameter(name, GroupTypeId.Data, SpecTypeId.Int.Integer, false);
#endif

    private static Level RequireLevel(Document document) =>
        new FilteredElementCollector(document)
            .OfClass(typeof(Level))
            .Cast<Level>()
            .FirstOrDefault()
        ?? throw new InvalidOperationException("No level was found in the test document.");

    private static View RequireView(Document document)
    {
        View[] views = new FilteredElementCollector(document)
            .OfClass(typeof(View))
            .Cast<View>()
            .Where(view => !view.IsTemplate)
            .ToArray();
        return views.FirstOrDefault()
            ?? throw new InvalidOperationException(
                "No view was found in the test document. " +
                $"Available views: {string.Join(", ", views.Select(view => $"'{view.Name}'"))}.");
    }
}
