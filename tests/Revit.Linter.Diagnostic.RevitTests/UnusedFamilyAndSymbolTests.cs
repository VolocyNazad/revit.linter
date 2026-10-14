using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.ElementDiagnostics.Diagnostics.FamilyUnused;
using Revit.Linter.ElementDiagnostics.Diagnostics.FamilyTypeUnused;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class UnusedFamilyAndSymbolTests : RevitApiTest
{
    private Document? _document;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() => _document = Application.NewProjectDocument(UnitSystem.Metric);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _document?.Close(false);

    [Test]
    public async Task Family_without_instances_is_reported()
    {
        Document document = _document!;
        Family family = FamilyTestDocuments.LoadFamilyWithType(Application, document, "LinterT1");
        var diagnostic = new FamilyUnusedDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, family);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Family_with_instance_is_valid()
    {
        Document document = _document!;
        Family family = FamilyTestDocuments.LoadFamilyWithType(Application, document, "LinterT1");
        FamilySymbol symbol = FindSymbol(document, "LinterT1");
        Level level = CreateLevel(document);
        using (Transaction transaction = new(document, "Place test family instance"))
        {
            transaction.Start();
            FamilyTestDocuments.PlaceInstance(document, symbol, level, XYZ.Zero);
            transaction.Commit();
        }
        var diagnostic = new FamilyUnusedDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, family);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Symbol_without_instances_is_reported()
    {
        Document document = _document!;
        _ = FamilyTestDocuments.LoadFamilyWithType(Application, document, "LinterT1");
        FamilySymbol symbol = FindSymbol(document, "LinterT1");
        var diagnostic = new FamilySymbolUnusedDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, symbol);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Symbol_with_instance_is_valid()
    {
        Document document = _document!;
        _ = FamilyTestDocuments.LoadFamilyWithType(Application, document, "LinterT1");
        FamilySymbol symbol = FindSymbol(document, "LinterT1");
        Level level = CreateLevel(document);
        using (Transaction transaction = new(document, "Place test family instance"))
        {
            transaction.Start();
            FamilyTestDocuments.PlaceInstance(document, symbol, level, XYZ.Zero);
            transaction.Commit();
        }
        var diagnostic = new FamilySymbolUnusedDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, symbol);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Unused_checks_run_in_any_document()
    {
        var familyFilter = new FamilyUnusedDiagnosticDocumentFilter();
        var symbolFilter = new FamilySymbolUnusedDiagnosticDocumentFilter();
        await Assert.That(familyFilter.IsRelevantFor(_document!)).IsTrue();
        await Assert.That(symbolFilter.IsRelevantFor(_document!)).IsTrue();

        Document familyDocument = FamilyTestDocuments.OpenModelFamilyTemplate(Application);
        try
        {
            await Assert.That(familyFilter.IsRelevantFor(familyDocument)).IsTrue();
            await Assert.That(symbolFilter.IsRelevantFor(familyDocument)).IsTrue();
        }
        finally
        {
            familyDocument.Close(false);
        }
    }

    private static FamilySymbol FindSymbol(Document document, string typeName) =>
        FamilyTestDocuments.FindSymbolByTypeName(document, typeName);

    private static Level CreateLevel(Document document)
    {
        using Transaction transaction = new(document, "Create test level");
        transaction.Start();
        Level level = Level.Create(document, 0);
        transaction.Commit();
        return level;
    }
}
