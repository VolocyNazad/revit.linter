using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.ElementDiagnostics.Diagnostics.GroupExists;
using Revit.Linter.ElementDiagnostics.Diagnostics.GroupNested;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class GroupFixTests : RevitApiTest
{
    private Document? _document;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() => _document = Application.NewProjectDocument(UnitSystem.Metric);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _document?.Close(false);

    [Test]
    public async Task Ungroup_dissolves_group_and_keeps_members()
    {
        Document document = _document!;
        Group group = CreateGroup(document);
        ElementId groupId = group.Id;
        var fix = new UngroupGroup();

        bool result;
        using (Transaction transaction = new(document, "Ungroup test group"))
        {
            transaction.Start();
            result = fix.Execute(group);
            transaction.Commit();
        }

        await Assert.That(fix.Identity.Code).IsEqualTo("SHRD0027");
        await Assert.That(result).IsTrue();
        await Assert.That(document.GetElement(groupId)).IsNull();
        await Assert.That(CountGroups(document)).IsEqualTo(0);
        await Assert.That(CountMembers(document)).IsEqualTo(2);
    }

    [Test]
    public async Task Ungroup_nested_group_dissolves_inner_group()
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
        ElementId innerId = inner.Id;
        ElementId outerId = new FilteredElementCollector(document)
            .OfClass(typeof(Group))
            .Cast<Group>()
            .First(candidate => candidate.Id != innerId).Id;
        var fix = new UngroupNestedGroup();

        await Assert.That(inner.GroupId).IsNotEqualTo(ElementId.InvalidElementId);

        bool result;
        using (Transaction transaction = new(document, "Ungroup test nested group"))
        {
            transaction.Start();
            result = fix.Execute(inner);
            transaction.Commit();
        }

        await Assert.That(fix.Identity.Code).IsEqualTo("SHRD0021");
        await Assert.That(result).IsTrue();
        await Assert.That(document.GetElement(innerId)).IsNull();
        await Assert.That(document.GetElement(outerId)).IsNotNull();
        await Assert.That(CountGroups(document)).IsEqualTo(1);
    }

    [Test]
    public async Task Non_group_element_is_rejected()
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
        var fix = new UngroupGroup();

        bool result;
        using (Transaction transaction = new(document, "Ungroup test element"))
        {
            transaction.Start();
            result = fix.Execute(target);
            transaction.Commit();
        }

        await Assert.That(result).IsFalse();
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

    private static int CountGroups(Document document) =>
        new FilteredElementCollector(document).OfClass(typeof(Group)).GetElementCount();

    private static int CountMembers(Document document) =>
        new FilteredElementCollector(document).OfClass(typeof(DirectShape)).GetElementCount();
}
