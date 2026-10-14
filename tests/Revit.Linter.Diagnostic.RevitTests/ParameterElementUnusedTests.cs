using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.ElementDiagnostics.Diagnostics.ParameterElementUnused;
using Revit.Linter.Testing;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class ParameterElementUnusedTests : RevitApiTest
{
    private Document? _document;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() => _document = Application.NewProjectDocument(UnitSystem.Metric);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _document?.Close(false);

    [Test]
    public async Task Unreferenced_global_parameter_is_reported()
    {
        Document document = _document!;
        GlobalParameter parameter = CreateGlobalParameter(document);
        var diagnostic = new ParameterElementUnusedDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, parameter);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Only_parameter_elements_are_checked_by_filter()
    {
        Document document = _document!;
        GlobalParameter parameter = CreateGlobalParameter(document);
        Level level = CreateLevel(document);
        var filter = new ParameterElementUnusedDiagnosticFilter();

        await Assert.That(filter.IsRelevantFor(document, parameter)).IsTrue();
        await Assert.That(filter.IsRelevantFor(document, level)).IsFalse();
    }

    [Test]
    public async Task Check_runs_in_projects_only()
    {
        var filter = new ParameterElementUnusedDiagnosticDocumentFilter();
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

    private static GlobalParameter CreateGlobalParameter(Document document)
    {
        using Transaction transaction = new(document, "Create test global parameter");
        transaction.Start();
#if IS2021
        GlobalParameter parameter = GlobalParameter.Create(document, "LinterParameter", ParameterType.Text);
#else
        GlobalParameter parameter = GlobalParameter.Create(document, "LinterParameter", SpecTypeId.String.Text);
#endif
        transaction.Commit();
        return parameter;
    }

    private static Level CreateLevel(Document document)
    {
        using Transaction transaction = new(document, "Create test level");
        transaction.Start();
        Level level = Level.Create(document, 0);
        transaction.Commit();
        return level;
    }
}
