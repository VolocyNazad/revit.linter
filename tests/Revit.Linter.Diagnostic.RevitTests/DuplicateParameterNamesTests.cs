using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.ElementDiagnostics.Diagnostics.DuplicateParameterNames;
using Revit.Linter.Testing;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class DuplicateParameterNamesTests : RevitApiTest
{
    private const string DuplicateName = "LinterDuplicateElementParameter";

    private Document? _document;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() => _document = Application.NewProjectDocument(UnitSystem.Metric);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _document?.Close(false);

    [Test]
    public async Task Family_type_parameter_with_project_parameter_is_reported()
    {
        Document document = _document!;
        Family family = LoadFamily(DuplicateName, isFamilyInstance: false);
        AddProjectParameter(family.FamilyCategory, DuplicateName, Guid.NewGuid(), isInstance: true);
        PlaceInstance(family, CreateLevel());
        var diagnostic = new DuplicateParameterNamesDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, family).Single();

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That((string)feedback.AdditionalMessageArguments!["parameter.name"]).IsEqualTo(DuplicateName);
        await Assert.That(feedback.AdditionalMessageArguments["parameter.count"]).IsEqualTo(2);
        await Assert.That(feedback.AdditionalMessageArguments["parameter.boundCount"]).IsEqualTo(1);
    }

    [Test]
    public async Task Family_instance_parameter_with_project_parameter_is_reported()
    {
        Document document = _document!;
        Family family = LoadFamily(DuplicateName, isFamilyInstance: true);
        AddProjectParameter(family.FamilyCategory, DuplicateName, Guid.NewGuid(), isInstance: true);
        PlaceInstance(family, CreateLevel());
        var diagnostic = new DuplicateParameterNamesDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, family).Single();

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That(feedback.AdditionalMessageArguments!["parameter.count"]).IsEqualTo(2);
        await Assert.That(feedback.AdditionalMessageArguments["parameter.boundCount"]).IsEqualTo(1);
    }

    [Test]
    public async Task Family_type_parameter_with_type_project_parameter_is_reported()
    {
        Document document = _document!;
        Family family = LoadFamily(DuplicateName, isFamilyInstance: false);
        AddProjectParameter(family.FamilyCategory, DuplicateName, Guid.NewGuid(), isInstance: false);
        var diagnostic = new DuplicateParameterNamesDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, family).Single();

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That(feedback.AdditionalMessageArguments!["parameter.count"]).IsEqualTo(2);
    }

    [Test]
    public async Task Family_type_parameter_is_reported_without_a_placed_instance()
    {
        Document document = _document!;
        Family family = LoadFamily(DuplicateName, isFamilyInstance: false);
        AddProjectParameter(family.FamilyCategory, DuplicateName, Guid.NewGuid(), isInstance: true);
        var diagnostic = new DuplicateParameterNamesDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, family).Single();

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Several_types_and_instances_produce_one_finding()
    {
        Document document = _document!;
        Family family = LoadFamily(DuplicateName, isFamilyInstance: false, typeNames: ["LinterTypeA", "LinterTypeB"]);
        AddProjectParameter(family.FamilyCategory, DuplicateName, Guid.NewGuid(), isInstance: true);
        Level level = CreateLevel();
        foreach (FamilySymbol symbol in SymbolsOf(family)) PlaceInstance(symbol, level);
        var diagnostic = new DuplicateParameterNamesDiagnostic(TestDocumentQueries.Create());

        IReadOnlyList<DiagnosticFeedback> feedbacks = diagnostic.Execute(document, null, family);

        await Assert.That(feedbacks.Count).IsEqualTo(1);
        await Assert.That(feedbacks[0].Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Same_shared_parameter_in_family_and_project_is_valid()
    {
        Document document = _document!;
        Guid guid = Guid.NewGuid();
        Family family = LoadSharedFamily(guid, DuplicateName, isFamilyInstance: false);
        AddProjectParameter(family.FamilyCategory, DuplicateName, guid, isInstance: true);
        var diagnostic = new DuplicateParameterNamesDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, family).Single();

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Two_project_parameters_with_one_name_are_left_to_the_document_check()
    {
        Document document = _document!;
        Family family = LoadFamily("LinterFamilyNote", isFamilyInstance: false);
        using (Transaction transaction = new(document, "Add duplicate project parameters"))
        {
            transaction.Start();
            SharedParameterTestFile.Add(document, DuplicateName, Guid.NewGuid(), family.FamilyCategory, true);
            SharedParameterTestFile.Add(document, DuplicateName, Guid.NewGuid(), family.FamilyCategory, true);
            transaction.Commit();
        }
        var diagnostic = new DuplicateParameterNamesDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, family).Single();

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Project_parameter_bound_to_another_category_is_valid()
    {
        Document document = _document!;
        Family family = LoadFamily(DuplicateName, isFamilyInstance: false);
        Category walls = Category.GetCategory(document, BuiltInCategory.OST_Walls);
        AddProjectParameter(walls, DuplicateName, Guid.NewGuid(), isInstance: true);
        var diagnostic = new DuplicateParameterNamesDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, family).Single();

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Family_with_distinct_parameter_names_is_valid()
    {
        Document document = _document!;
        Family family = LoadFamily("LinterFamilyParameter", isFamilyInstance: false);
        AddProjectParameter(family.FamilyCategory, "LinterProjectParameter", Guid.NewGuid(), isInstance: true);
        PlaceInstance(family, CreateLevel());
        var diagnostic = new DuplicateParameterNamesDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, family).Single();

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Check_runs_in_project_documents_only()
    {
        var filter = new DuplicateParameterNamesDiagnosticDocumentFilter();

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
    public async Task Only_families_are_checked()
    {
        Document document = _document!;
        Family family = LoadFamily(DuplicateName, isFamilyInstance: false);
        Level level = CreateLevel();
        FamilyInstance instance = PlaceInstance(family, level);
        var filter = new DuplicateParameterNamesDiagnosticFilter();

        await Assert.That(filter.IsRelevantFor(document, family)).IsTrue();
        await Assert.That(filter.IsRelevantFor(document, level)).IsFalse();
        await Assert.That(filter.IsRelevantFor(document, instance)).IsFalse();
    }

    private Family LoadFamily(string parameterName, bool isFamilyInstance, string[]? typeNames = null)
    {
        Document familyDocument = FamilyTestDocuments.OpenModelFamilyTemplate(Application);
        try
        {
            using (Transaction transaction = new(familyDocument, "Add test family parameter"))
            {
                transaction.Start();
                AddFamilyParameter(familyDocument, parameterName, isFamilyInstance);
                foreach (string typeName in typeNames ?? [])
                    familyDocument.FamilyManager.NewType(typeName);
                transaction.Commit();
            }
            return familyDocument.LoadFamily(_document!);
        }
        finally
        {
            familyDocument.Close(false);
        }
    }

    private Family LoadSharedFamily(Guid guid, string parameterName, bool isFamilyInstance)
    {
        Document familyDocument = FamilyTestDocuments.OpenModelFamilyTemplate(Application);
        try
        {
            using (Transaction transaction = new(familyDocument, "Add shared family parameter"))
            {
                transaction.Start();
                SharedParameterTestFile.AddToFamily(familyDocument, parameterName, guid, isFamilyInstance);
                transaction.Commit();
            }
            return familyDocument.LoadFamily(_document!);
        }
        finally
        {
            familyDocument.Close(false);
        }
    }

    private static void AddFamilyParameter(Document familyDocument, string parameterName, bool isFamilyInstance)
    {
        FamilyManager manager = familyDocument.FamilyManager;
#if IS2021
        manager.AddParameter(parameterName, BuiltInParameterGroup.PG_DATA, ParameterType.Text, isFamilyInstance);
#else
        manager.AddParameter(parameterName, GroupTypeId.Data, SpecTypeId.String.Text, isFamilyInstance);
#endif
    }

    private void AddProjectParameter(Category category, string name, Guid guid, bool isInstance)
    {
        Document document = _document!;
        using Transaction transaction = new(document, "Add test project parameter");
        transaction.Start();
        SharedParameterTestFile.Add(document, name, guid, category, isInstance);
        transaction.Commit();
    }

    private Level CreateLevel()
    {
        Document document = _document!;
        using Transaction transaction = new(document, "Create test level");
        transaction.Start();
        Level level = Level.Create(document, 0);
        transaction.Commit();
        return level;
    }

    private static IEnumerable<FamilySymbol> SymbolsOf(Family family) =>
        new FilteredElementCollector(family.Document)
            .OfClass(typeof(FamilySymbol))
            .Cast<FamilySymbol>()
            .Where(symbol => symbol.Family.Id == family.Id);

    private FamilyInstance PlaceInstance(Family family, Level level) =>
        PlaceInstance(SymbolsOf(family).First(), level);

    private FamilyInstance PlaceInstance(FamilySymbol symbol, Level level)
    {
        Document document = _document!;
        using Transaction transaction = new(document, "Place test family instance");
        transaction.Start();
        FamilyInstance instance = FamilyTestDocuments.PlaceInstance(document, symbol, level, XYZ.Zero);
        transaction.Commit();
        return instance;
    }
}
