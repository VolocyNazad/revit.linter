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
    public async Task Family_and_project_parameter_with_one_name_are_reported()
    {
        Document document = _document!;
        Family family = LoadFamilyWithTextParameter(document, DuplicateName);
        // The project parameter is added after the family is loaded, which is the order that leaves
        // the family parameter and the project parameter side by side on the element.
        using (Transaction transaction = new(document, "Add duplicate test project parameter"))
        {
            transaction.Start();
            SharedParameterTestFile.Add(document, DuplicateName, Guid.NewGuid(), family.FamilyCategory);
            transaction.Commit();
        }
        FamilyInstance instance = PlaceInstance(document, family);
        var diagnostic = new DuplicateParameterNamesDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, instance).Single();

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That((string)feedback.AdditionalMessageArguments!["parameter.name"]).IsEqualTo(DuplicateName);
        await Assert.That(feedback.AdditionalMessageArguments["parameter.boundCount"]).IsEqualTo(1);
    }

    [Test]
    public async Task Family_instance_with_distinct_parameter_names_is_valid()
    {
        Document document = _document!;
        Family family = LoadFamilyWithTextParameter(document, "LinterFamilyParameter");
        using (Transaction transaction = new(document, "Add distinct test project parameter"))
        {
            transaction.Start();
            SharedParameterTestFile.Add(
                document, "LinterProjectParameter", Guid.NewGuid(), family.FamilyCategory);
            transaction.Commit();
        }
        FamilyInstance instance = PlaceInstance(document, family);
        var diagnostic = new DuplicateParameterNamesDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, instance).Single();

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
    public async Task Only_family_instances_are_checked()
    {
        Document document = _document!;
        Level level = CreateLevel(document);
        var filter = new DuplicateParameterNamesDiagnosticFilter();

        await Assert.That(filter.IsRelevantFor(document, level)).IsFalse();
    }

    private static Level CreateLevel(Document document)
    {
        using Transaction transaction = new(document, "Create test level");
        transaction.Start();
        Level level = Level.Create(document, 0);
        transaction.Commit();
        return level;
    }

    private static Family LoadFamilyWithTextParameter(Document projectDocument, string parameterName)
    {
        Document familyDocument = FamilyTestDocuments.OpenModelFamilyTemplate(projectDocument.Application);
        try
        {
            using (Transaction transaction = new(familyDocument, "Add test family parameter"))
            {
                transaction.Start();
                FamilyTestDocuments.AddTextTypeParameter(familyDocument, parameterName);
                transaction.Commit();
            }
            return familyDocument.LoadFamily(projectDocument);
        }
        finally
        {
            familyDocument.Close(false);
        }
    }

    private static FamilyInstance PlaceInstance(Document document, Family family)
    {
        Level level = CreateLevel(document);
        FamilySymbol symbol = new FilteredElementCollector(document)
            .OfClass(typeof(FamilySymbol))
            .Cast<FamilySymbol>()
            .First(candidate => candidate.Family.Id == family.Id);
        using Transaction transaction = new(document, "Place test family instance");
        transaction.Start();
        FamilyInstance instance = FamilyTestDocuments.PlaceInstance(document, symbol, level, XYZ.Zero);
        transaction.Commit();
        return instance;
    }
}
