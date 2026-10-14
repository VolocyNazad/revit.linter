using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.ElementDiagnostics.Diagnostics.UnfilledFamilyParameters;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class UnfilledFamilyParametersTests : RevitApiTest
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
    public async Task Family_with_empty_text_parameter_is_reported()
    {
        Document document = _familyDocument!;
        using (Transaction transaction = new(document, "Create test family type"))
        {
            transaction.Start();
            FamilyTestDocuments.AddTextTypeParameter(document, "Note");
            document.FamilyManager.NewType("T1");
            transaction.Commit();
        }
        var diagnostic = new UnfilledFamilyParametersDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, document.OwnerFamily);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That((string)feedback.AdditionalMessageArguments!["details"]).Contains("T1");
    }

    [Test]
    public async Task Family_with_filled_text_parameter_is_valid()
    {
        Document document = _familyDocument!;
        using (Transaction transaction = new(document, "Create test family type"))
        {
            transaction.Start();
            FamilyTestDocuments.AddTextTypeParameter(document, "Note");
            FamilyType type = document.FamilyManager.NewType("T1");
            document.FamilyManager.CurrentType = type;
            // Every text parameter counts, including the built-in identity ones:
            // a single empty cell keeps the family reported.
            foreach (FamilyParameter parameter in document.FamilyManager.Parameters
                .Cast<FamilyParameter>()
                .Where(parameter => parameter.StorageType == StorageType.String))
                document.FamilyManager.Set(parameter, "filled");
            transaction.Commit();
        }
        var diagnostic = new UnfilledFamilyParametersDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, document.OwnerFamily);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Only_owning_family_is_checked_by_filter()
    {
        // Same reason as in DuplicateFamilyTypes: nested families own their
        // parameters, invisible to the host FamilyManager.
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
        var filter = new UnfilledFamilyParametersDiagnosticFilter();

        await Assert.That(filter.IsRelevantFor(host, host.OwnerFamily)).IsTrue();
        await Assert.That(filter.IsRelevantFor(host, nestedFamily)).IsFalse();
    }

    [Test]
    public async Task Check_runs_in_family_documents_only()
    {
        var filter = new UnfilledFamilyParametersDiagnosticDocumentFilter();
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
}
