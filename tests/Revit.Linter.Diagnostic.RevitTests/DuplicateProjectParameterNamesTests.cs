using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.DocumentDiagnostics.Diagnostics.DuplicateProjectParameterNames;
using Revit.Linter.Testing;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class DuplicateProjectParameterNamesTests : RevitApiTest
{
    private const string DuplicateName = "LinterDuplicateProjectParameter";

    private Document? _document;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() => _document = Application.NewProjectDocument(UnitSystem.Metric);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _document?.Close(false);

    [Test]
    public async Task Two_shared_parameters_with_one_name_are_reported()
    {
        Document document = _document!;
        Category walls = Category.GetCategory(document, BuiltInCategory.OST_Walls);
        using (Transaction transaction = new(document, "Add duplicate test parameters"))
        {
            transaction.Start();
            // Two files, one name: Revit keeps both definitions because their GUIDs differ.
            SharedParameterTestFile.Add(document, DuplicateName, Guid.NewGuid(), walls);
            SharedParameterTestFile.Add(document, DuplicateName, Guid.NewGuid(), walls);
            transaction.Commit();
        }
        var diagnostic = new DuplicateProjectParameterNamesDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback feedback = diagnostic.Execute(document).Single();

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That((string)feedback.AdditionalMessageArguments!["parameter.name"]).IsEqualTo(DuplicateName);
        await Assert.That(feedback.AdditionalMessageArguments["parameter.count"]).IsEqualTo(2);
        // Both definitions are bound to the same category, so the category is listed once.
        string[] categories = ((IEnumerable<string>)feedback.AdditionalMessageArguments["parameter.categories"])
            .ToArray();
        await Assert.That(categories).Contains(walls.Name);
        await Assert.That(categories.Length).IsEqualTo(1);
        await Assert.That(feedback.MessageFormat).IsNull();
    }

    [Test]
    public async Task Category_bound_to_a_single_definition_is_not_listed()
    {
        Document document = _document!;
        Category walls = Category.GetCategory(document, BuiltInCategory.OST_Walls);
        Category doors = Category.GetCategory(document, BuiltInCategory.OST_Doors);
        using (Transaction transaction = new(document, "Add duplicate test parameters on separate categories"))
        {
            transaction.Start();
            // The definitions never meet on one element, so no category is shared.
            SharedParameterTestFile.Add(document, DuplicateName, Guid.NewGuid(), walls);
            SharedParameterTestFile.Add(document, DuplicateName, Guid.NewGuid(), doors);
            transaction.Commit();
        }
        var diagnostic = new DuplicateProjectParameterNamesDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback feedback = diagnostic.Execute(document).Single();

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        string[] categories = ((IEnumerable<string>)feedback.AdditionalMessageArguments!["parameter.categories"])
            .ToArray();
        await Assert.That(categories).IsEmpty();
        await Assert.That(feedback.MessageFormat).IsNotNull();
    }

    [Test]
    public async Task Parameters_with_different_names_are_valid()
    {
        Document document = _document!;
        Category walls = Category.GetCategory(document, BuiltInCategory.OST_Walls);
        using (Transaction transaction = new(document, "Add distinct test parameters"))
        {
            transaction.Start();
            SharedParameterTestFile.Add(document, "LinterProjectParameterA", Guid.NewGuid(), walls);
            SharedParameterTestFile.Add(document, "LinterProjectParameterB", Guid.NewGuid(), walls);
            transaction.Commit();
        }
        var diagnostic = new DuplicateProjectParameterNamesDiagnostic(TestDocumentQueries.Create());

        IReadOnlyList<DiagnosticFeedback> feedbacks = diagnostic.Execute(document).ToArray();

        await Assert.That(feedbacks).IsEmpty();
    }

    [Test]
    public async Task Check_runs_in_project_documents_only()
    {
        var filter = new DuplicateProjectParameterNamesDiagnosticFilter();

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
