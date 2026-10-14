using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.ElementDiagnostics.Diagnostics.GroupExists;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class GroupExistsTests : RevitApiTest
{
    private Document? _document;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() => _document = Application.NewProjectDocument(UnitSystem.Metric);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _document?.Close(false);

    [Test]
    public async Task Placed_group_is_reported()
    {
        Document document = _document!;
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
        Group group = new FilteredElementCollector(document)
            .OfClass(typeof(Group))
            .Cast<Group>()
            .Single();
        var diagnostic = new GroupExistsDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, group);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Only_groups_are_checked_by_filter()
    {
        Document document = _document!;
        DirectShape member;
        using (Transaction transaction = new(document, "Create test group"))
        {
            transaction.Start();
            ElementId categoryId = Category.GetCategory(document, BuiltInCategory.OST_GenericModel).Id;
            DirectShape first = DirectShape.CreateElement(document, categoryId);
            DirectShape second = DirectShape.CreateElement(document, categoryId);
            document.Regenerate();
            document.Create.NewGroup([first.Id, second.Id]);
            transaction.Commit();
            member = first;
        }
        Group group = new FilteredElementCollector(document)
            .OfClass(typeof(Group))
            .Cast<Group>()
            .Single();
        var filter = new GroupExistsDiagnosticFilter();

        await Assert.That(filter.IsRelevantFor(document, group)).IsTrue();
        await Assert.That(filter.IsRelevantFor(document, member)).IsFalse();
    }

    [Test]
    public async Task Check_runs_in_any_document()
    {
        var filter = new GroupExistsDiagnosticDocumentFilter();
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
}
