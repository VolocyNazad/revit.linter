using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.ElementDiagnostics.Diagnostics.DuplicateFamilyTypes;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class DuplicateFamilyTypesTests : RevitApiTest
{
    private Document? _familyDocument;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() =>
        _familyDocument = FamilyTestDocuments.OpenModelFamilyTemplate(Application);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _familyDocument?.Close(false);

    [Test]
    public async Task Identical_types_are_reported()
    {
        Document document = _familyDocument!;
        using (Transaction transaction = new(document, "Create duplicate family types"))
        {
            transaction.Start();
            FamilyTestDocuments.AddTextTypeParameter(document, "Note");
            document.FamilyManager.NewType("A");
            document.FamilyManager.NewType("B");
            transaction.Commit();
        }
        var diagnostic = new DuplicateFamilyTypesDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, document.OwnerFamily);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That((string)feedback.AdditionalMessageArguments!["duplicateNames"]).Contains("A = B");
    }

    [Test]
    public async Task Diverged_types_are_valid()
    {
        Document document = _familyDocument!;
        using (Transaction transaction = new(document, "Create diverged family types"))
        {
            transaction.Start();
            FamilyTestDocuments.AddTextTypeParameter(document, "Note");
            document.FamilyManager.NewType("A");
            FamilyType diverged = document.FamilyManager.NewType("B");
            document.FamilyManager.CurrentType = diverged;
            document.FamilyManager.Set(FindFamilyParameter(document, "Note"), "other");
            transaction.Commit();
        }
        var diagnostic = new DuplicateFamilyTypesDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, document.OwnerFamily);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Only_owning_family_is_checked_by_filter()
    {
        // Nested families carry their own types, invisible to the host
        // FamilyManager, so only the owning family is relevant.
        Document host = _familyDocument!;
        Document nested = FamilyTestDocuments.OpenModelFamilyTemplate(Application);
        Family nestedFamily;
        try
        {
            nestedFamily = nested.LoadFamily(host);
        }
        finally
        {
            nested.Close(false);
        }
        var filter = new DuplicateFamilyTypesDiagnosticFilter();

        await Assert.That(filter.IsRelevantFor(host, host.OwnerFamily)).IsTrue();
        await Assert.That(filter.IsRelevantFor(host, nestedFamily)).IsFalse();
    }

    [Test]
    public async Task Check_runs_in_family_documents_only()
    {
        var filter = new DuplicateFamilyTypesDiagnosticDocumentFilter();
        await Assert.That(filter.IsRelevantFor(_familyDocument!)).IsTrue();

        Document projectDocument = Application.NewProjectDocument(UnitSystem.Metric);
        try
        {
            await Assert.That(filter.IsRelevantFor(projectDocument)).IsFalse();
        }
        finally
        {
            projectDocument.Close(false);
        }
    }

    private static FamilyParameter FindFamilyParameter(Document document, string name) =>
        document.FamilyManager.Parameters
            .Cast<FamilyParameter>()
            .First(candidate => candidate.Definition.Name == name);
}
