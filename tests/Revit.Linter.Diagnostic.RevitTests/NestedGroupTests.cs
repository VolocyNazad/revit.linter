using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.ElementDiagnostics.Diagnostics.GroupNested;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class NestedGroupTests : RevitApiTest
{
    private Document? _document;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() => _document = Application.NewProjectDocument(UnitSystem.Metric);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _document?.Close(false);

    [Test]
    public async Task Top_level_group_is_valid()
    {
        Document document = _document!;
        Group group = CreateGroup(document);
        var diagnostic = new GroupNestedDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, group);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Nested_group_is_reported()
    {
        Document document = _document!;
        Group inner = CreateGroup(document);
        using (Transaction transaction = new(document, "Create test outer group"))
        {
            transaction.Start();
            ElementId categoryId = Category.GetCategory(document, BuiltInCategory.OST_GenericModel).Id;
            DirectShape shape = DirectShape.CreateElement(document, categoryId);
            document.Regenerate();
            document.Create.NewGroup([shape.Id, inner.Id]);
            transaction.Commit();
        }
        inner = (Group)document.GetElement(inner.Id)
            ?? throw new InvalidOperationException("The test inner group was not found.");
        var diagnostic = new GroupNestedDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, inner);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Only_groups_are_checked_by_filter()
    {
        Document document = _document!;
        Group group = CreateGroup(document);
        Element member = new FilteredElementCollector(document)
            .OfClass(typeof(DirectShape))
            .Cast<DirectShape>()
            .First();
        var filter = new GroupNestedDiagnosticFilter();

        await Assert.That(filter.IsRelevantFor(document, group)).IsTrue();
        await Assert.That(filter.IsRelevantFor(document, member)).IsFalse();
    }

    [Test]
    public async Task Check_runs_in_any_document()
    {
        var filter = new GroupNestedDiagnosticDocumentFilter();
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

    private static Group CreateGroup(Document document)
    {
        using (Transaction transaction = new(document, "Create test group"))
        {
            transaction.Start();
            ElementId categoryId = Category.GetCategory(document, BuiltInCategory.OST_GenericModel).Id;
            DirectShape first = DirectShape.CreateElement(document, categoryId);
            DirectShape second = DirectShape.CreateElement(document, categoryId);
            // Newly created members must be regenerated before grouping: otherwise
            // the grouping is rolled back and the returned reference is not valid.
            // The group is collected afresh after the commit instead of trusting it.
            document.Regenerate();
            document.Create.NewGroup([first.Id, second.Id]);
            transaction.Commit();
        }
        return new FilteredElementCollector(document)
            .OfClass(typeof(Group))
            .Cast<Group>()
            .Single();
    }
}
