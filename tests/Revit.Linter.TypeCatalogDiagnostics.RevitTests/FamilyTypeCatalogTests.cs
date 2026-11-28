using System.Globalization;
using System.Text;
using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.Testing;
using Revit.Linter.TypeCatalogDiagnostics.Diagnostics.FamilyTypeCatalog;
using Revit.Linter.TypeCatalogDiagnostics.Diagnostics.FamilyTypeCatalogInUse;
using Revit.Linter.TypeCatalogDiagnostics.Diagnostics.FamilyTypeCatalogMissingParameters;
using Revit.Linter.TypeCatalogDiagnostics.Diagnostics.FamilyTypesMissingFromCatalog;
using Revit.Linter.TypeCatalogDiagnostics.Diagnostics.ManyFamilyTypes;
using TUnit.Core.Executors;

namespace Revit.Linter.TypeCatalogDiagnostics.RevitTests;

public sealed class FamilyTypeCatalogTests : RevitApiTest
{
    // The default type is the type that carries the family name, and a family names itself after
    // its file.
    private const string FamilyFileName = "LinterCatalogFamily";

    private Document? _familyDocument;
    private string? _directory;
    private string? _familyPath;
    private int _nextTypeIndex;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument()
    {
        _directory = Path.Combine(
            Path.GetTempPath(), nameof(FamilyTypeCatalogTests), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _familyDocument = TestFamilyDocuments.OpenModelFamilyTemplate(Application);
        EnsureDefaultType(_familyDocument, FamilyFileName);
        _familyPath = Path.Combine(_directory, FamilyFileName + ".rfa");
        _familyDocument.SaveAs(_familyPath);
    }

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument()
    {
        try
        {
            _familyDocument?.Close(false);
        }
        finally
        {
            if (_directory is not null && Directory.Exists(_directory))
                Directory.Delete(_directory, recursive: true);
        }
    }

    [Test]
    public async Task Threshold_counts_types_beyond_the_default_type()
    {
        Document document = _familyDocument!;
        AddTypes(document, ManyFamilyTypesDiagnostic.Threshold - 1);
        var diagnostic = new ManyFamilyTypesDiagnostic();

        DiagnosticFeedback belowThreshold = diagnostic.Execute(document, null, document.OwnerFamily).Single();

        AddTypes(document, 1);
        DiagnosticFeedback atThreshold = diagnostic.Execute(document, null, document.OwnerFamily).Single();

        await Assert.That(belowThreshold.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
        await Assert.That(atThreshold.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That((int)atThreshold.AdditionalMessageArguments!["typeCount"])
            .IsEqualTo(ManyFamilyTypesDiagnostic.Threshold);
        await Assert.That(atThreshold.MessageFormat)
            .IsEqualTo(TypeCatalogDiagnosticLocalizations.GetString("SHRD0046_message"));
    }

    [Test]
    public async Task Below_threshold_with_catalog_is_valid()
    {
        Document document = _familyDocument!;
        AddTypes(document, ManyFamilyTypesDiagnostic.Threshold - 1);
        WriteCatalog("CatalogOnly");
        var diagnostic = new ManyFamilyTypesDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, document.OwnerFamily).Single();

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Blank_and_family_named_types_are_not_own_types()
    {
        await Assert.That(FamilyTypeCatalogReader.IsOwnTypeName(" ", "LinterCatalogFamily")).IsFalse();
        await Assert.That(FamilyTypeCatalogReader.IsOwnTypeName(null, "LinterCatalogFamily")).IsFalse();
        await Assert.That(FamilyTypeCatalogReader.IsOwnTypeName("LinterCatalogFamily", "LinterCatalogFamily")).IsFalse();
        await Assert.That(FamilyTypeCatalogReader.IsOwnTypeName("LinterA", "LinterCatalogFamily")).IsTrue();
    }

    [Test]
    public async Task Many_types_with_catalog_are_reported()
    {
        Document document = _familyDocument!;
        AddTypes(document, ManyFamilyTypesDiagnostic.Threshold);
        WriteCatalog("CatalogOnly");
        var diagnostic = new ManyFamilyTypesDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, document.OwnerFamily).Single();

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That(feedback.MessageFormat)
            .IsEqualTo(TypeCatalogDiagnosticLocalizations.GetString("SHRD0046_message_with_catalog"));
        Dictionary<string, object> arguments = feedback.AdditionalMessageArguments!;
        await Assert.That((int)arguments["typeCount"]).IsEqualTo(ManyFamilyTypesDiagnostic.Threshold);
        await Assert.That((string)arguments["catalogName"]).IsEqualTo("LinterCatalogFamily.txt");
        await Assert.That((string)arguments["familyName"]).IsEqualTo("LinterCatalogFamily");
    }

    [Test]
    public async Task Family_type_missing_from_the_catalog_is_reported()
    {
        Document document = _familyDocument!;
        AddTypes(document, "LinterA");
        WriteCatalog("LinterCatalogFamily", "CatalogOnly");
        var diagnostic = new FamilyTypesMissingFromCatalogDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, document.OwnerFamily).Single();

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        Dictionary<string, object> arguments = feedback.AdditionalMessageArguments!;
        await Assert.That((string)arguments["typeNames"]).IsEqualTo("LinterA");
        await Assert.That((string)arguments["catalogName"]).IsEqualTo("LinterCatalogFamily.txt");
        await Assert.That((string)arguments["familyName"]).IsEqualTo("LinterCatalogFamily");
    }

    [Test]
    public async Task Family_type_listed_in_the_catalog_is_not_reported()
    {
        Document document = _familyDocument!;
        AddTypes(document, "LinterA");
        WriteCatalog("LinterCatalogFamily", "LinterA");
        var diagnostic = new FamilyTypesMissingFromCatalogDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, document.OwnerFamily).Single();

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Missing_check_is_valid_without_catalog()
    {
        Document document = _familyDocument!;
        AddTypes(document, "LinterA");
        var diagnostic = new FamilyTypesMissingFromCatalogDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, document.OwnerFamily).Single();

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Catalog_without_rows_is_named_but_not_compared()
    {
        Document document = _familyDocument!;
        AddTypes(document, ManyFamilyTypesDiagnostic.Threshold);
        WriteCatalog();

        DiagnosticFeedback many = new ManyFamilyTypesDiagnostic()
            .Execute(document, null, document.OwnerFamily).Single();
        DiagnosticFeedback missing = new FamilyTypesMissingFromCatalogDiagnostic()
            .Execute(document, null, document.OwnerFamily).Single();
        DiagnosticFeedback inUse = new FamilyTypeCatalogInUseDiagnostic()
            .Execute(document, null, document.OwnerFamily).Single();

        await Assert.That(many.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That(many.MessageFormat)
            .IsEqualTo(TypeCatalogDiagnosticLocalizations.GetString("SHRD0046_message_with_catalog"));
        await Assert.That(missing.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
        await Assert.That(inUse.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Unreadable_catalog_is_named_but_not_compared()
    {
        Document document = _familyDocument!;
        AddTypes(document, ManyFamilyTypesDiagnostic.Threshold);
        WriteCatalog("CatalogOnly");

        using (File.Open(CatalogPath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            DiagnosticFeedback many = new ManyFamilyTypesDiagnostic()
                .Execute(document, null, document.OwnerFamily).Single();
            DiagnosticFeedback missing = new FamilyTypesMissingFromCatalogDiagnostic()
                .Execute(document, null, document.OwnerFamily).Single();
            DiagnosticFeedback inUse = new FamilyTypeCatalogInUseDiagnostic()
                .Execute(document, null, document.OwnerFamily).Single();

            await Assert.That(many.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
            await Assert.That(many.MessageFormat)
                .IsEqualTo(TypeCatalogDiagnosticLocalizations.GetString("SHRD0046_message_with_catalog"));
            await Assert.That((string)many.AdditionalMessageArguments!["catalogName"]).IsEqualTo("LinterCatalogFamily.txt");
            await Assert.That(missing.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
            await Assert.That(inUse.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
        }
    }

    [Test]
    public async Task Quoted_catalog_name_with_a_quote_character_is_read_as_one_name()
    {
        Document document = _familyDocument!;
        AddTypes(document, "Linter\"A");
        WriteCatalog("LinterCatalogFamily", "Linter\"A");
        var diagnostic = new FamilyTypesMissingFromCatalogDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, document.OwnerFamily).Single();

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Only_types_missing_from_the_catalog_are_listed()
    {
        Document document = _familyDocument!;
        AddTypes(document, "LinterA", "LinterB");
        WriteCatalog("LinterCatalogFamily", "LinterA");
        var diagnostic = new FamilyTypesMissingFromCatalogDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, document.OwnerFamily).Single();

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That((string)feedback.AdditionalMessageArguments!["typeNames"]).IsEqualTo("LinterB");
    }

    [Test]
    public async Task Default_type_missing_from_the_catalog_is_reported()
    {
        Document document = _familyDocument!;
        WriteCatalog("CatalogOnly");
        var diagnostic = new FamilyTypesMissingFromCatalogDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, document.OwnerFamily).Single();

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That((string)feedback.AdditionalMessageArguments!["typeNames"]).IsEqualTo("LinterCatalogFamily");
    }

    [Test]
    public async Task Quoted_catalog_name_with_comma_is_read_as_one_name()
    {
        Document document = _familyDocument!;
        AddTypes(document, "Linter, A");
        WriteCatalog("LinterCatalogFamily", "Linter, A");
        var diagnostic = new FamilyTypesMissingFromCatalogDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, document.OwnerFamily).Single();

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Own_types_with_catalog_are_reported_as_used_together()
    {
        Document document = _familyDocument!;
        AddTypes(document, "LinterA");
        WriteCatalog("CatalogOnly");
        var diagnostic = new FamilyTypeCatalogInUseDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, document.OwnerFamily).Single();

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        Dictionary<string, object> arguments = feedback.AdditionalMessageArguments!;
        await Assert.That((int)arguments["ownTypeCount"]).IsEqualTo(1);
        await Assert.That((int)arguments["catalogTypeCount"]).IsEqualTo(1);
        await Assert.That((string)arguments["familyName"]).IsEqualTo("LinterCatalogFamily");
    }

    [Test]
    public async Task Own_type_listed_in_the_catalog_still_means_two_approaches()
    {
        Document document = _familyDocument!;
        AddTypes(document, "LinterA");
        WriteCatalog("LinterA");
        var diagnostic = new FamilyTypeCatalogInUseDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, document.OwnerFamily).Single();

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That((int)feedback.AdditionalMessageArguments!["ownTypeCount"]).IsEqualTo(1);
    }

    [Test]
    public async Task Base_type_with_catalog_is_valid()
    {
        Document document = _familyDocument!;
        WriteCatalog("CatalogOnly");
        var diagnostic = new FamilyTypeCatalogInUseDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, document.OwnerFamily).Single();

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task One_type_with_any_name_with_catalog_is_valid()
    {
        Document document = _familyDocument!;
        RenameTheOnlyType(document, "LinterBase");
        WriteCatalog("CatalogOnly");
        var diagnostic = new FamilyTypeCatalogInUseDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, document.OwnerFamily).Single();

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Family_parameter_missing_from_the_catalog_is_reported()
    {
        Document document = _familyDocument!;
        AddLengthParameter(document, "Height");
        WriteCatalog("CatalogOnly");
        var diagnostic = new FamilyTypeCatalogMissingParametersDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, document.OwnerFamily).Single();

        Dictionary<string, object> arguments = feedback.AdditionalMessageArguments!;
        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That((string)arguments["parameterNames"]).IsEqualTo("Height");
        await Assert.That((string)arguments["catalogName"]).IsEqualTo("LinterCatalogFamily.txt");
        await Assert.That((string)arguments["familyName"]).IsEqualTo("LinterCatalogFamily");
    }

    [Test]
    public async Task Family_parameter_listed_in_the_catalog_is_valid()
    {
        Document document = _familyDocument!;
        AddLengthParameter(document, "Height");
        WriteCatalogWithParameters("Height##length##millimeters", "CatalogOnly");
        var diagnostic = new FamilyTypeCatalogMissingParametersDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, document.OwnerFamily).Single();

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Instance_parameter_is_not_required_in_the_catalog()
    {
        Document document = _familyDocument!;
        AddLengthParameter(document, "Height", isInstance: true);
        WriteCatalog("CatalogOnly");
        var diagnostic = new FamilyTypeCatalogMissingParametersDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, document.OwnerFamily).Single();

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Formula_parameter_is_not_required_in_the_catalog()
    {
        Document document = _familyDocument!;
        AddLengthParameter(document, "Width");
        FamilyParameter height = AddLengthParameter(document, "Height");
        SetFormula(document, height, "Width * 2");
        WriteCatalog("CatalogOnly");
        var diagnostic = new FamilyTypeCatalogMissingParametersDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, document.OwnerFamily).Single();

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Fix_creates_a_catalog_and_removes_the_types()
    {
        Document document = _familyDocument!;
        FamilyParameter height = AddLengthParameter(document, "Height");
        FamilyParameter finish = AddTextParameter(document, "Finish");
        FamilyParameter count = AddIntegerParameter(document, "Count");
        FamilyParameter isOpen = AddYesNoParameter(document, "IsOpen");
        FamilyParameter material = AddMaterialParameter(document, "FinishMaterial");
        ElementId materialId = CreateMaterial(document, "LinterMaterial");
        AddTypes(document, "Linter1", "Linter2", "Linter3", "Linter4", "Linter5", "Linter6");
        SetValues(
            document,
            "Linter1",
            (height, UnitUtils.ConvertToInternalUnits(1000, UnitTypeId.Millimeters)),
            (finish, "Red, matte"),
            (count, 3),
            (isOpen, 1),
            (material, materialId));
        string activeName = document.FamilyManager.CurrentType.Name;

        var fix = new CreateCatalogAndRemoveTypes();
        await Assert.That(fix is IElementFixConfirmation).IsTrue();
        bool applied = ExecuteFix(fix, document);

        await Assert.That(applied).IsTrue();
        await Assert.That(document.FamilyManager.Types.Size).IsEqualTo(1);
        await Assert.That(document.FamilyManager.Types.Cast<FamilyType>().Single().Name).IsEqualTo(activeName);
        IReadOnlyList<string> lines = ReadCatalogLines();
        await Assert.That(ReadCatalogValue(lines, "Linter1", "Finish")).IsEqualTo("Red, matte");
        await Assert.That(ReadCatalogValue(lines, "Linter1", "Count")).IsEqualTo("3");
        await Assert.That(ReadCatalogValue(lines, "Linter1", "IsOpen")).IsEqualTo("1");
        await Assert.That(ReadCatalogValue(lines, "Linter1", "FinishMaterial")).IsEqualTo("LinterMaterial");
        await Assert.That(Math.Abs(ReadCatalogLength(lines, "Linter1", "Height") - 1000)).IsLessThan(0.01);
        await Assert.That(lines.Any(line => line.StartsWith("Linter6,", StringComparison.Ordinal))).IsTrue();
        await Assert.That(new ManyFamilyTypesDiagnostic().Execute(document, null, document.OwnerFamily).Single().Verdict)
            .IsEqualTo(DiagnosticVerdict.Valid);
        await Assert.That(new FamilyTypesMissingFromCatalogDiagnostic().Execute(document, null, document.OwnerFamily).Single().Verdict)
            .IsEqualTo(DiagnosticVerdict.Valid);
        await Assert.That(new FamilyTypeCatalogMissingParametersDiagnostic().Execute(document, null, document.OwnerFamily).Single().Verdict)
            .IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Fix_adds_missing_types_to_the_catalog_and_removes_them()
    {
        Document document = _familyDocument!;
        FamilyParameter height = AddLengthParameter(document, "Height");
        AddTypes(document, "LinterA");
        SetValues(
            document,
            "LinterA",
            (height, UnitUtils.ConvertToInternalUnits(1000, UnitTypeId.Millimeters)));
        WriteCatalogWithParameters("Height##LENGTH##MILLIMETERS", "LinterCatalogFamily");

        var fix = new AddMissingTypesToCatalog();
        await Assert.That(fix is IElementFixConfirmation).IsTrue();
        bool applied = ExecuteFix(fix, document);

        await Assert.That(applied).IsTrue();
        await Assert.That(document.FamilyManager.Types.Size).IsEqualTo(1);
        IReadOnlyList<string> lines = ReadCatalogLines();
        await Assert.That(Math.Abs(ReadCatalogLength(lines, "LinterA", "Height") - 1000)).IsLessThan(0.01);
        await Assert.That(new FamilyTypesMissingFromCatalogDiagnostic().Execute(document, null, document.OwnerFamily).Single().Verdict)
            .IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Fix_adds_missing_parameter_columns_to_the_catalog()
    {
        Document document = _familyDocument!;
        FamilyParameter height = AddLengthParameter(document, "Height");
        SetValues(
            document,
            "LinterCatalogFamily",
            (height, UnitUtils.ConvertToInternalUnits(1000, UnitTypeId.Millimeters)));
        WriteCatalog("LinterCatalogFamily");

        var fix = new AddMissingParametersToCatalog();
        await Assert.That(fix is IElementFixConfirmation).IsTrue();
        bool applied = ExecuteFix(fix, document);

        await Assert.That(applied).IsTrue();
        IReadOnlyList<string> lines = ReadCatalogLines();
        await Assert.That(FamilyTypeCatalogCsv.ReadFields(lines[0]).Any(field => field.StartsWith("Height##", StringComparison.Ordinal))).IsTrue();
        await Assert.That(Math.Abs(ReadCatalogLength(lines, "LinterCatalogFamily", "Height") - 1000)).IsLessThan(0.01);
        await Assert.That(new FamilyTypeCatalogMissingParametersDiagnostic().Execute(document, null, document.OwnerFamily).Single().Verdict)
            .IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Fix_moves_catalog_types_into_the_family()
    {
        Document document = _familyDocument!;
        FamilyParameter height = AddLengthParameter(document, "Height");
        FamilyParameter finish = AddTextParameter(document, "Finish");
        FamilyParameter count = AddIntegerParameter(document, "Count");
        FamilyParameter isOpen = AddYesNoParameter(document, "IsOpen");
        FamilyParameter material = AddMaterialParameter(document, "FinishMaterial");
        FamilyParameter rotation = AddAngleParameter(document, "Rotation");
        FamilyParameter price = AddCurrencyParameter(document, "Price");
        ElementId materialId = CreateMaterial(document, "LinterMaterial");
        AddTypes(document, "LinterOwn");
        WriteCatalogLines(
            CatalogHeader(height, finish, count, isOpen, material, rotation, price),
            CatalogRow("LinterFromCatalog", "1000", "Red, matte", "3", "1", "LinterMaterial", "45", "12.5"));

        var fix = new MoveCatalogTypesToFamily();
        await Assert.That(fix is IElementFixConfirmation).IsTrue();
        bool applied = ExecuteFix(fix, document);

        await Assert.That(applied).IsTrue();
        await Assert.That(File.Exists(CatalogPath)).IsFalse();
        FamilyType moved = FindFamilyType(document, "LinterFromCatalog");
        await Assert.That(Math.Abs(moved.AsDouble(height)!.Value
            - UnitUtils.ConvertToInternalUnits(1000, UnitTypeId.Millimeters))).IsLessThan(0.01);
        await Assert.That(moved.AsString(finish)).IsEqualTo("Red, matte");
        await Assert.That(moved.AsInteger(count)).IsEqualTo(3);
        await Assert.That(moved.AsInteger(isOpen)).IsEqualTo(1);
        await Assert.That(moved.AsElementId(material)).IsEqualTo(materialId);
        await Assert.That(Math.Abs(moved.AsDouble(rotation)!.Value
            - UnitUtils.ConvertToInternalUnits(45, UnitTypeId.Degrees))).IsLessThan(0.0001);
        await Assert.That(moved.AsDouble(price)!.Value).IsEqualTo(12.5);
        await Assert.That(new FamilyTypeCatalogInUseDiagnostic()
            .Execute(document, null, document.OwnerFamily).Single().Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Fix_keeps_the_family_values_for_a_type_the_catalog_also_lists()
    {
        Document document = _familyDocument!;
        FamilyParameter height = AddLengthParameter(document, "Height");
        AddTypes(document, "LinterA");
        SetValues(document, "LinterA",
            (height, UnitUtils.ConvertToInternalUnits(500, UnitTypeId.Millimeters)));
        WriteCatalogLines(
            CatalogHeader(height),
            CatalogRow("LinterA", "1000"),
            CatalogRow("LinterB", "2000"));

        bool applied = ExecuteFix(new MoveCatalogTypesToFamily(), document);

        await Assert.That(applied).IsTrue();
        await Assert.That(File.Exists(CatalogPath)).IsFalse();
        FamilyType own = FindFamilyType(document, "LinterA");
        await Assert.That(Math.Abs(own.AsDouble(height)!.Value
            - UnitUtils.ConvertToInternalUnits(500, UnitTypeId.Millimeters))).IsLessThan(0.01);
        FamilyType moved = FindFamilyType(document, "LinterB");
        await Assert.That(Math.Abs(moved.AsDouble(height)!.Value
            - UnitUtils.ConvertToInternalUnits(2000, UnitTypeId.Millimeters))).IsLessThan(0.01);
    }

    [Test]
    public async Task Fix_does_not_change_the_family_when_the_catalog_cannot_be_deleted()
    {
        Document document = _familyDocument!;
        FamilyParameter height = AddLengthParameter(document, "Height");
        AddTypes(document, "LinterOwn");
        WriteCatalogLines(
            CatalogHeader(height),
            CatalogRow("LinterFromCatalog", "1000"));

        bool applied;
        using (File.Open(CatalogPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            applied = ExecuteFix(new MoveCatalogTypesToFamily(), document);
        }

        await Assert.That(applied).IsFalse();
        await Assert.That(document.FamilyManager.Types.Size).IsEqualTo(2);
        await Assert.That(File.Exists(CatalogPath)).IsTrue();
        await Assert.That(document.FamilyManager.Types.Cast<FamilyType>()
            .Any(type => string.Equals(type.Name, "LinterFromCatalog", StringComparison.Ordinal))).IsFalse();
    }

    [Test]
    public async Task Fix_moves_family_types_into_the_catalog()
    {
        Document document = _familyDocument!;
        FamilyParameter height = AddLengthParameter(document, "Height");
        AddTypes(document, "LinterA", "LinterB");
        SetValues(document, "LinterA",
            (height, UnitUtils.ConvertToInternalUnits(1000, UnitTypeId.Millimeters)));
        SetValues(document, "LinterB",
            (height, UnitUtils.ConvertToInternalUnits(2000, UnitTypeId.Millimeters)));
        WriteCatalogLines(
            CatalogHeader(height),
            CatalogRow("LinterCatalogFamily", "500"),
            CatalogRow("LinterA", "500"));

        var fix = new MoveFamilyTypesToCatalog();
        await Assert.That(fix is IElementFixConfirmation).IsTrue();
        bool applied = ExecuteFix(fix, document);

        await Assert.That(applied).IsTrue();
        await Assert.That(document.FamilyManager.Types.Size).IsEqualTo(1);
        IReadOnlyList<string> lines = ReadCatalogLines();
        await Assert.That(ReadCatalogDouble(lines, "LinterA", "Height")).IsEqualTo(500);
        await Assert.That(ReadCatalogDouble(lines, "LinterB", "Height")).IsEqualTo(2000);
        await Assert.That(new FamilyTypeCatalogInUseDiagnostic()
            .Execute(document, null, document.OwnerFamily).Single().Verdict).IsEqualTo(DiagnosticVerdict.Valid);
        await Assert.That(new FamilyTypesMissingFromCatalogDiagnostic()
            .Execute(document, null, document.OwnerFamily).Single().Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Fix_moves_family_types_into_the_catalog_when_every_row_exists()
    {
        Document document = _familyDocument!;
        FamilyParameter height = AddLengthParameter(document, "Height");
        AddTypes(document, "LinterA");
        SetValues(document, "LinterA",
            (height, UnitUtils.ConvertToInternalUnits(1000, UnitTypeId.Millimeters)));
        WriteCatalogLines(
            CatalogHeader(height),
            CatalogRow("LinterCatalogFamily", "500"),
            CatalogRow("LinterA", "1000"));

        bool applied = ExecuteFix(new MoveFamilyTypesToCatalog(), document);

        await Assert.That(applied).IsTrue();
        await Assert.That(document.FamilyManager.Types.Size).IsEqualTo(1);
        await Assert.That(ReadCatalogLines().Count).IsEqualTo(3);
        await Assert.That(new FamilyTypeCatalogInUseDiagnostic()
            .Execute(document, null, document.OwnerFamily).Single().Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Created_catalog_loads_a_type_with_its_values()
    {
        Document document = _familyDocument!;
        FamilyParameter height = AddLengthParameter(document, "Height");
        FamilyParameter finish = AddTextParameter(document, "Finish");
        FamilyParameter isOpen = AddYesNoParameter(document, "IsOpen");
        AddTypes(document, "Linter1", "Linter2", "Linter3", "Linter4", "Linter5", "Linter6");
        SetValues(
            document,
            "Linter1",
            (height, UnitUtils.ConvertToInternalUnits(1000, UnitTypeId.Millimeters)),
            (finish, "Red, matte"),
            (isOpen, 1));
        MakeTypeCurrent(document, "Linter2");

        await Assert.That(ExecuteFix(new CreateCatalogAndRemoveTypes(), document)).IsTrue();
        document.Save();

        Document project = Application.NewProjectDocument(UnitSystem.Metric);
        try
        {
            using Transaction transaction = new(project, "Load a catalog type");
            transaction.Start();
            bool loaded = project.LoadFamilySymbol(_familyPath!, "Linter1");
            transaction.Commit();

            await Assert.That(loaded).IsTrue();
            FamilySymbol symbol = new FilteredElementCollector(project)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .First(candidate => string.Equals(candidate.Name, "Linter1", StringComparison.Ordinal));
            Parameter? heightParameter = symbol.LookupParameter("Height");
            await Assert.That(heightParameter).IsNotNull();
            await Assert.That(Math.Abs(
                heightParameter.AsDouble() - UnitUtils.ConvertToInternalUnits(1000, UnitTypeId.Millimeters)))
                .IsLessThan(0.01);
            await Assert.That(symbol.LookupParameter("Finish")?.AsString()).IsEqualTo("Red, matte");
            await Assert.That(symbol.LookupParameter("IsOpen")?.AsInteger()).IsEqualTo(1);
        }
        finally
        {
            project.Close(false);
        }
    }

    [Test]
    public async Task Appended_catalog_row_loads_with_its_values()
    {
        Document document = _familyDocument!;
        FamilyParameter height = AddLengthParameter(document, "Height");
        AddTypes(document, "LinterA");
        SetValues(
            document,
            "LinterA",
            (height, UnitUtils.ConvertToInternalUnits(1000, UnitTypeId.Millimeters)));
        WriteCatalogWithParameters("Height##LENGTH##MILLIMETERS", "LinterCatalogFamily");

        await Assert.That(ExecuteFix(new AddMissingTypesToCatalog(), document)).IsTrue();
        document.Save();

        Document project = Application.NewProjectDocument(UnitSystem.Metric);
        try
        {
            using Transaction transaction = new(project, "Load a catalog type");
            transaction.Start();
            bool loaded = project.LoadFamilySymbol(_familyPath!, "LinterA");
            transaction.Commit();

            await Assert.That(loaded).IsTrue();
            FamilySymbol symbol = new FilteredElementCollector(project)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .First(candidate => string.Equals(candidate.Name, "LinterA", StringComparison.Ordinal));
            Parameter? heightParameter = symbol.LookupParameter("Height");
            await Assert.That(heightParameter).IsNotNull();
            await Assert.That(Math.Abs(
                heightParameter.AsDouble() - UnitUtils.ConvertToInternalUnits(1000, UnitTypeId.Millimeters)))
                .IsLessThan(0.01);
        }
        finally
        {
            project.Close(false);
        }
    }

    [Test]
    public async Task Fix_does_not_change_the_family_when_the_catalog_cannot_be_written()
    {
        Document document = _familyDocument!;
        AddTypes(document, "Linter1", "Linter2", "Linter3", "Linter4", "Linter5", "Linter6");
        WriteCatalog("CatalogOnly");

        bool applied;
        using (File.Open(CatalogPath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            applied = ExecuteFix(new CreateCatalogAndRemoveTypes(), document);
        }

        await Assert.That(applied).IsFalse();
        await Assert.That(document.FamilyManager.Types.Size).IsEqualTo(7);
        IReadOnlyList<string> lines = ReadCatalogLines();
        await Assert.That(lines.Any(line => line.Contains("CatalogOnly", StringComparison.Ordinal))).IsTrue();
    }

    [Test]
    public async Task Fix_adds_a_column_and_keeps_existing_quoted_values()
    {
        Document document = _familyDocument!;
        FamilyParameter height = AddLengthParameter(document, "Height");
        SetValues(
            document,
            "LinterCatalogFamily",
            (height, UnitUtils.ConvertToInternalUnits(1000, UnitTypeId.Millimeters)));
        WriteCatalogLines(
            ",Width##LENGTH##MILLIMETERS,Finish##OTHER##",
            "LinterCatalogFamily,1000,\"Red, matte\"");

        bool applied = ExecuteFix(new AddMissingParametersToCatalog(), document);

        await Assert.That(applied).IsTrue();
        IReadOnlyList<string> lines = ReadCatalogLines();
        IReadOnlyList<string> row = FamilyTypeCatalogCsv.ReadFields(
            lines.Single(line => line.StartsWith("LinterCatalogFamily,", StringComparison.Ordinal)));
        await Assert.That(row[1]).IsEqualTo("1000");
        await Assert.That(row[2]).IsEqualTo("Red, matte");
        await Assert.That(FamilyTypeCatalogCsv.ReadFields(lines[0])
            .Any(field => field.StartsWith("Height##", StringComparison.Ordinal))).IsTrue();
    }

    [Test]
    public async Task Fix_adds_the_default_type_row_without_removing_the_type()
    {
        Document document = _familyDocument!;
        WriteCatalog("CatalogOnly");

        bool applied = ExecuteFix(new AddMissingTypesToCatalog(), document);

        await Assert.That(applied).IsTrue();
        await Assert.That(document.FamilyManager.Types.Size).IsEqualTo(1);
        IReadOnlyList<string> lines = ReadCatalogLines();
        await Assert.That(lines.Any(line => line.StartsWith("LinterCatalogFamily,", StringComparison.Ordinal))).IsTrue();
        await Assert.That(new FamilyTypesMissingFromCatalogDiagnostic()
            .Execute(document, null, document.OwnerFamily).Single().Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Fix_writes_angle_and_currency_headers()
    {
        Document document = _familyDocument!;
        FamilyParameter rotation = AddAngleParameter(document, "Rotation");
        FamilyParameter price = AddCurrencyParameter(document, "Price");
        AddTypes(document, "Linter1");
        SetValues(
            document,
            "Linter1",
            (rotation, UnitUtils.ConvertToInternalUnits(90, UnitTypeId.Degrees)),
            (price, 12.5));

        bool applied = ExecuteFix(new CreateCatalogAndRemoveTypes(), document);

        await Assert.That(applied).IsTrue();
        IReadOnlyList<string> lines = ReadCatalogLines();
        IReadOnlyList<string> header = FamilyTypeCatalogCsv.ReadFields(lines[0]);
        await Assert.That(header.Any(field => field.StartsWith("Rotation##ANGLE##", StringComparison.Ordinal))).IsTrue();
        await Assert.That(header.Any(field => string.Equals(field, "Price##CURRENCY##", StringComparison.Ordinal))).IsTrue();
        string rotationHeader = header.First(field => field.StartsWith("Rotation##ANGLE##", StringComparison.Ordinal));
        string unitToken = rotationHeader.Split(new[] { "##" }, StringSplitOptions.None)[2];
        ForgeTypeId angleUnit = FamilyTypeCatalogUnits.FindUnit(unitToken) ?? UnitTypeId.Degrees;
        double expectedAngle = UnitUtils.ConvertFromInternalUnits(
            UnitUtils.ConvertToInternalUnits(90, UnitTypeId.Degrees), angleUnit);
        await Assert.That(Math.Abs(ReadCatalogDouble(lines, "Linter1", "Rotation") - expectedAngle))
            .IsLessThan(0.001);
        await Assert.That(Math.Abs(ReadCatalogDouble(lines, "Linter1", "Price") - 12.5)).IsLessThan(0.001);
    }

    [Test]
    public async Task Own_types_without_catalog_are_valid()
    {
        Document document = _familyDocument!;
        AddTypes(document, "LinterA");
        var diagnostic = new FamilyTypeCatalogInUseDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, document.OwnerFamily).Single();

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Unsaved_family_is_not_checked()
    {
        Document unsaved = TestFamilyDocuments.OpenModelFamilyTemplate(Application);
        try
        {
            AddTypes(unsaved, ManyFamilyTypesDiagnostic.Threshold);
            DiagnosticFeedback[] feedbacks =
            [
                new ManyFamilyTypesDiagnostic().Execute(unsaved, null, unsaved.OwnerFamily).Single(),
                new FamilyTypesMissingFromCatalogDiagnostic().Execute(unsaved, null, unsaved.OwnerFamily).Single(),
                new FamilyTypeCatalogInUseDiagnostic().Execute(unsaved, null, unsaved.OwnerFamily).Single(),
                new FamilyTypeCatalogMissingParametersDiagnostic().Execute(unsaved, null, unsaved.OwnerFamily).Single(),
            ];

            await Assert.That(feedbacks.All(feedback => feedback.Verdict == DiagnosticVerdict.Valid)).IsTrue();
        }
        finally
        {
            unsaved.Close(false);
        }
    }

    [Test]
    public async Task Family_checks_are_hidden_in_a_project_document()
    {
        Document project = Application.NewProjectDocument(UnitSystem.Metric);
        try
        {
            await Assert.That(new ManyFamilyTypesDiagnosticDocumentFilter().IsRelevantFor(project)).IsFalse();
            await Assert.That(new FamilyTypesMissingFromCatalogDiagnosticDocumentFilter().IsRelevantFor(project)).IsFalse();
            await Assert.That(new FamilyTypeCatalogInUseDiagnosticDocumentFilter().IsRelevantFor(project)).IsFalse();
            await Assert.That(new FamilyTypeCatalogMissingParametersDiagnosticDocumentFilter().IsRelevantFor(project)).IsFalse();
            await Assert.That(new ManyFamilyTypesDiagnosticDocumentFilter().IsRelevantFor(_familyDocument!)).IsTrue();
            await Assert.That(new FamilyTypesMissingFromCatalogDiagnosticDocumentFilter().IsRelevantFor(_familyDocument!)).IsTrue();
            await Assert.That(new FamilyTypeCatalogInUseDiagnosticDocumentFilter().IsRelevantFor(_familyDocument!)).IsTrue();
            await Assert.That(new FamilyTypeCatalogMissingParametersDiagnosticDocumentFilter().IsRelevantFor(_familyDocument!)).IsTrue();
        }
        finally
        {
            project.Close(false);
        }
    }

    [Test]
    public async Task Only_the_owning_family_is_checked()
    {
        Document document = _familyDocument!;
        AddTypes(document, "LinterA");
        var filter = new FamilyTypeCatalogInUseDiagnosticFilter();
        Element unrelated = new FilteredElementCollector(document)
            .OfClass(typeof(ReferencePlane))
            .FirstElement();

        await Assert.That(filter.IsRelevantFor(document, document.OwnerFamily)).IsTrue();
        await Assert.That(filter.IsRelevantFor(document, unrelated)).IsFalse();
        await Assert.That(new ManyFamilyTypesDiagnosticFilter().IsRelevantFor(document, document.OwnerFamily)).IsTrue();
        await Assert.That(new ManyFamilyTypesDiagnosticFilter().IsRelevantFor(document, unrelated)).IsFalse();
        await Assert.That(new FamilyTypesMissingFromCatalogDiagnosticFilter().IsRelevantFor(document, document.OwnerFamily)).IsTrue();
        await Assert.That(new FamilyTypesMissingFromCatalogDiagnosticFilter().IsRelevantFor(document, unrelated)).IsFalse();
        await Assert.That(new FamilyTypeCatalogMissingParametersDiagnosticFilter().IsRelevantFor(document, document.OwnerFamily)).IsTrue();
        await Assert.That(new FamilyTypeCatalogMissingParametersDiagnosticFilter().IsRelevantFor(document, unrelated)).IsFalse();
    }

    [Test]
    public async Task Family_template_is_not_checked()
    {
        Document template = TestFamilyDocuments.OpenModelFamilyTemplate(Application);
        string path = Path.Combine(_directory!, "LinterCatalogTemplate.rft");
        try
        {
            AddTypes(template, ManyFamilyTypesDiagnostic.Threshold);
            template.SaveAs(path);
            DiagnosticFeedback[] feedbacks =
            [
                new ManyFamilyTypesDiagnostic().Execute(template, null, template.OwnerFamily).Single(),
                new FamilyTypesMissingFromCatalogDiagnostic().Execute(template, null, template.OwnerFamily).Single(),
                new FamilyTypeCatalogInUseDiagnostic().Execute(template, null, template.OwnerFamily).Single(),
            ];

            await Assert.That(Path.GetExtension(template.PathName)).IsEqualTo(".rft");
            await Assert.That(feedbacks.All(feedback => feedback.Verdict == DiagnosticVerdict.Valid)).IsTrue();
        }
        finally
        {
            template.Close(false);
        }
    }

    private static void RenameTheOnlyType(Document document, string name)
    {
        using Transaction transaction = new(document, "Rename the only family type");
        transaction.Start();
        FamilyType only = document.FamilyManager.Types.Cast<FamilyType>().Single();
        document.FamilyManager.CurrentType = only;
        document.FamilyManager.RenameCurrentType(name);
        transaction.Commit();
    }

    private static void EnsureDefaultType(Document document, string familyName)
    {
        using Transaction transaction = new(document, "Keep one family type");
        transaction.Start();
        FamilyManager manager = document.FamilyManager;
        if (manager.Types.Size == 0)
        {
            manager.NewType(familyName);
            transaction.Commit();
            return;
        }
        while (manager.Types.Size > 1)
        {
            FamilyType last = manager.Types.Cast<FamilyType>().Last();
            manager.CurrentType = last;
            manager.DeleteCurrentType();
        }
        FamilyType? only = manager.CurrentType ?? manager.Types.Cast<FamilyType>().FirstOrDefault();
        if (only is not null)
        {
            manager.CurrentType = only;
            manager.RenameCurrentType(familyName);
        }
        transaction.Commit();
    }

    private void AddTypes(Document document, int count)
    {
        string[] names = new string[count];
        for (int index = 0; index < count; index++)
            names[index] = $"LinterType{++_nextTypeIndex}";
        AddTypes(document, names);
    }

    private static void AddTypes(Document document, params string[] names)
    {
        using Transaction transaction = new(document, "Create family types");
        transaction.Start();
        foreach (string name in names)
            document.FamilyManager.NewType(name);
        transaction.Commit();
    }

    private void WriteCatalog(params string[] typeNames)
    {
        string header = ",Width##length##millimeters";
        string[] lines = [header, .. typeNames.Select(name => $"{Escape(name)},1000")];
        File.WriteAllLines(CatalogPath, lines, new UnicodeEncoding(bigEndian: false, byteOrderMark: true));
    }

    private void WriteCatalogWithParameters(string parameterHeader, params string[] typeNames)
    {
        string[] lines = [$",{parameterHeader}", .. typeNames.Select(name => $"{Escape(name)},1000")];
        File.WriteAllLines(CatalogPath, lines, new UnicodeEncoding(bigEndian: false, byteOrderMark: true));
    }

    private void WriteCatalogLines(params string[] lines) =>
        File.WriteAllLines(CatalogPath, lines, new UnicodeEncoding(bigEndian: false, byteOrderMark: true));

    private static string CatalogHeader(params FamilyParameter[] parameters)
    {
        List<string> fields = [string.Empty];
        fields.AddRange(parameters.Select(parameter => FamilyTypeCatalogColumn.FromParameter(parameter).Header));
        return FamilyTypeCatalogCsv.Join(fields);
    }

    private static string CatalogRow(string typeName, params string[] values)
    {
        List<string> fields = [FamilyTypeCatalogCsv.FormatField(typeName)];
        fields.AddRange(values.Select(FamilyTypeCatalogCsv.FormatField));
        return FamilyTypeCatalogCsv.Join(fields);
    }

    private static FamilyType FindFamilyType(Document document, string name) =>
        document.FamilyManager.Types
            .Cast<FamilyType>()
            .First(type => string.Equals(type.Name, name, StringComparison.Ordinal));

    private static FamilyParameter AddLengthParameter(Document document, string name, bool isInstance = false)
    {
        using Transaction transaction = new(document, "Create length parameter");
        transaction.Start();
        FamilyManager manager = document.FamilyManager;
#if IS2021
        FamilyParameter parameter = manager.AddParameter(
            name, BuiltInParameterGroup.PG_GEOMETRY, ParameterType.Length, isInstance);
#else
        FamilyParameter parameter = manager.AddParameter(
            name, GroupTypeId.Geometry, SpecTypeId.Length, isInstance);
#endif
        transaction.Commit();
        return parameter;
    }

    private static void SetFormula(Document document, FamilyParameter parameter, string formula)
    {
        using Transaction transaction = new(document, "Set parameter formula");
        transaction.Start();
        document.FamilyManager.SetFormula(parameter, formula);
        transaction.Commit();
    }

    private static FamilyParameter AddTextParameter(Document document, string name)
    {
        using Transaction transaction = new(document, "Create text parameter");
        transaction.Start();
        FamilyManager manager = document.FamilyManager;
#if IS2021
        FamilyParameter parameter = manager.AddParameter(
            name, BuiltInParameterGroup.PG_DATA, ParameterType.Text, false);
#else
        FamilyParameter parameter = manager.AddParameter(name, GroupTypeId.Data, SpecTypeId.String.Text, false);
#endif
        transaction.Commit();
        return parameter;
    }

    private static FamilyParameter AddIntegerParameter(Document document, string name)
    {
        using Transaction transaction = new(document, "Create integer parameter");
        transaction.Start();
        FamilyManager manager = document.FamilyManager;
#if IS2021
        FamilyParameter parameter = manager.AddParameter(
            name, BuiltInParameterGroup.PG_DATA, ParameterType.Integer, false);
#else
        FamilyParameter parameter = manager.AddParameter(name, GroupTypeId.Data, SpecTypeId.Int.Integer, false);
#endif
        transaction.Commit();
        return parameter;
    }

    private static FamilyParameter AddYesNoParameter(Document document, string name)
    {
        using Transaction transaction = new(document, "Create yes/no parameter");
        transaction.Start();
        FamilyManager manager = document.FamilyManager;
#if IS2021
        FamilyParameter parameter = manager.AddParameter(
            name, BuiltInParameterGroup.PG_DATA, ParameterType.YesNo, false);
#else
        FamilyParameter parameter = manager.AddParameter(name, GroupTypeId.Data, SpecTypeId.Boolean.YesNo, false);
#endif
        transaction.Commit();
        return parameter;
    }

    private static FamilyParameter AddMaterialParameter(Document document, string name)
    {
        using Transaction transaction = new(document, "Create material parameter");
        transaction.Start();
        FamilyManager manager = document.FamilyManager;
#if IS2021
        FamilyParameter parameter = manager.AddParameter(
            name, BuiltInParameterGroup.PG_MATERIALS, ParameterType.Material, false);
#else
        FamilyParameter parameter = manager.AddParameter(
            name, GroupTypeId.Materials, SpecTypeId.Reference.Material, false);
#endif
        transaction.Commit();
        return parameter;
    }

    private static FamilyParameter AddAngleParameter(Document document, string name)
    {
        using Transaction transaction = new(document, "Create angle parameter");
        transaction.Start();
        FamilyManager manager = document.FamilyManager;
#if IS2021
        FamilyParameter parameter = manager.AddParameter(
            name, BuiltInParameterGroup.PG_GEOMETRY, ParameterType.Angle, false);
#else
        FamilyParameter parameter = manager.AddParameter(name, GroupTypeId.Geometry, SpecTypeId.Angle, false);
#endif
        transaction.Commit();
        return parameter;
    }

    private static FamilyParameter AddCurrencyParameter(Document document, string name)
    {
        using Transaction transaction = new(document, "Create currency parameter");
        transaction.Start();
        FamilyManager manager = document.FamilyManager;
#if IS2021
        FamilyParameter parameter = manager.AddParameter(
            name, BuiltInParameterGroup.PG_DATA, ParameterType.Currency, false);
#else
        FamilyParameter parameter = manager.AddParameter(name, GroupTypeId.Data, SpecTypeId.Currency, false);
#endif
        transaction.Commit();
        return parameter;
    }

    private static void MakeTypeCurrent(Document document, string typeName)
    {
        using Transaction transaction = new(document, "Make a family type current");
        transaction.Start();
        document.FamilyManager.CurrentType = document.FamilyManager.Types
            .Cast<FamilyType>()
            .First(type => string.Equals(type.Name, typeName, StringComparison.Ordinal));
        transaction.Commit();
    }

    private static ElementId CreateMaterial(Document document, string name)
    {
        using Transaction transaction = new(document, "Create material");
        transaction.Start();
        ElementId materialId = Material.Create(document, name);
        transaction.Commit();
        return materialId;
    }

    private static void SetValues(
        Document document,
        string typeName,
        params (FamilyParameter Parameter, object Value)[] values)
    {
        using Transaction transaction = new(document, "Set family type values");
        transaction.Start();
        FamilyManager manager = document.FamilyManager;
        manager.CurrentType = manager.Types
            .Cast<FamilyType>()
            .First(type => string.Equals(type.Name, typeName, StringComparison.Ordinal));
        foreach ((FamilyParameter parameter, object value) in values)
        {
            switch (value)
            {
                case double number:
                    manager.Set(parameter, number);
                    break;
                case int number:
                    manager.Set(parameter, number);
                    break;
                case ElementId id:
                    manager.Set(parameter, id);
                    break;
                default:
                    manager.Set(parameter, (string)value);
                    break;
            }
        }
        transaction.Commit();
    }

    private static bool ExecuteFix(IElementFix fix, Document document)
    {
        using Transaction transaction = new(document, "Apply family catalog fix");
        transaction.Start();
        bool applied = fix.Execute(document.OwnerFamily);
        if (applied)
            transaction.Commit();
        else
            transaction.RollBack();
        return applied;
    }

    private static string? ReadCatalogValue(
        IReadOnlyList<string> lines,
        string typeName,
        string parameterName)
    {
        IReadOnlyList<string> header = FamilyTypeCatalogCsv.ReadFields(lines[0]);
        int column = -1;
        for (int index = 1; index < header.Count; index++)
        {
            string name = header[index].Split(new[] { "##" }, StringSplitOptions.None)[0];
            if (!string.Equals(name, parameterName, StringComparison.Ordinal)) continue;
            column = index;
            break;
        }
        if (column < 0) return null;

        foreach (string line in lines.Skip(1))
        {
            IReadOnlyList<string> fields = FamilyTypeCatalogCsv.ReadFields(line);
            if (fields.Count > 0 && string.Equals(fields[0], typeName, StringComparison.Ordinal))
                return fields.Count > column ? fields[column] : null;
        }
        return null;
    }

    private static double ReadCatalogLength(
        IReadOnlyList<string> lines,
        string typeName,
        string parameterName) =>
        ReadCatalogDouble(lines, typeName, parameterName);

    private static double ReadCatalogDouble(
        IReadOnlyList<string> lines,
        string typeName,
        string parameterName) =>
        double.Parse(ReadCatalogValue(lines, typeName, parameterName) ?? "0", CultureInfo.InvariantCulture);

    private IReadOnlyList<string> ReadCatalogLines()
    {
#pragma warning disable S6966 // ReadAllLinesAsync does not exist on net48, where this test also runs.
        return File.ReadAllLines(CatalogPath);
#pragma warning restore S6966
    }

    private string CatalogPath => Path.ChangeExtension(_familyPath!, ".txt");

    private static string Escape(string typeName) =>
        typeName.Contains(',') || typeName.Contains('"')
            ? $"\"{typeName.Replace("\"", "\"\"")}\""
            : typeName;
}
