using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.ElementDiagnostics.Diagnostics.UnsharedFamilyParameters;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class UnsharedFamilyParametersTests : RevitApiTest
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
    public async Task Family_parameter_that_is_not_shared_is_reported()
    {
        Document document = _familyDocument!;
        using (Transaction transaction = new(document, "Add test family parameter"))
        {
            transaction.Start();
            FamilyTestDocuments.AddTextTypeParameter(document, "Note");
            transaction.Commit();
        }
        var diagnostic = new UnsharedFamilyParametersDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, document.OwnerFamily).Single();

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That(ReadNames(feedback)).Contains("Note");
    }

    [Test]
    public async Task Shared_family_parameter_is_not_reported()
    {
        Document document = _familyDocument!;
        using (Transaction transaction = new(document, "Add test shared family parameter"))
        {
            transaction.Start();
            SharedParameterTestFile.AddToFamily(document, "SharedNote", Guid.NewGuid());
            transaction.Commit();
        }
        var diagnostic = new UnsharedFamilyParametersDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, document.OwnerFamily).Single();

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Only_the_unshared_parameter_is_named()
    {
        Document document = _familyDocument!;
        using (Transaction transaction = new(document, "Add test family parameters"))
        {
            transaction.Start();
            FamilyTestDocuments.AddTextTypeParameter(document, "Note");
            SharedParameterTestFile.AddToFamily(document, "SharedNote", Guid.NewGuid());
            transaction.Commit();
        }
        var diagnostic = new UnsharedFamilyParametersDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, document.OwnerFamily).Single();

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        string[] names = ReadNames(feedback);
        await Assert.That(names).Contains("Note");
        await Assert.That(names).DoesNotContain("SharedNote");
    }

    private static string[] ReadNames(DiagnosticFeedback feedback) =>
        ((IEnumerable<string>)feedback.AdditionalMessageArguments!["parameter.names"]).ToArray();

    [Test]
    public async Task Check_runs_in_family_documents_only()
    {
        var filter = new UnsharedFamilyParametersDiagnosticDocumentFilter();

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
