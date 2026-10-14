using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.ElementDiagnostics.Diagnostics.DuplicateFamilySymbols;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class DuplicateFamilySymbolsTests : RevitApiTest
{
    private Document? _document;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() => _document = Application.NewProjectDocument(UnitSystem.Metric);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _document?.Close(false);

    [Test]
    public async Task Identical_symbols_are_reported()
    {
        Document document = _document!;
        LoadFamilyWithTypes(document, "Note", "A", "B");
        FamilySymbol first = FindSymbol(document, "A");
        FamilySymbol second = FindSymbol(document, "B");
        var diagnostic = new DuplicateFamilySymbolsDiagnostic();

        DiagnosticFeedback firstFeedback = diagnostic.Execute(document, null, first);
        DiagnosticFeedback secondFeedback = diagnostic.Execute(document, null, second);

        await Assert.That(firstFeedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That(secondFeedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That((string)firstFeedback.AdditionalMessageArguments!["duplicateNames"])
            .IsEqualTo("B");
        await Assert.That((string)secondFeedback.AdditionalMessageArguments!["duplicateNames"])
            .IsEqualTo("A");
    }

    [Test]
    public async Task Diverged_symbols_are_valid()
    {
        Document document = _document!;
        LoadFamilyWithTypes(document, "Note", "A", "B");
        FamilySymbol second = FindSymbol(document, "B");
        using (Transaction transaction = new(document, "Diverge test family symbols"))
        {
            transaction.Start();
            second.LookupParameter("Note").Set("other");
            transaction.Commit();
        }
        FamilySymbol first = FindSymbol(document, "A");
        var diagnostic = new DuplicateFamilySymbolsDiagnostic();

        DiagnosticFeedback firstFeedback = diagnostic.Execute(document, null, first);
        DiagnosticFeedback secondFeedback = diagnostic.Execute(document, null, second);

        await Assert.That(firstFeedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
        await Assert.That(secondFeedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Symbols_from_different_families_are_valid()
    {
        // A duplicate is a type with the same parameters in the same family:
        // identical values in different families are independent types, not copies.
        Document document = _document!;
        FamilyTestDocuments.LoadFamilyWithType(Application, document, "LinterT1");
        FamilyTestDocuments.LoadFamilyWithType(Application, document, "LinterT1");
        List<FamilySymbol> symbols = new FilteredElementCollector(document)
            .OfClass(typeof(FamilySymbol))
            .Cast<FamilySymbol>()
            .Where(candidate => candidate.Name == "LinterT1")
            .ToList();
        var diagnostic = new DuplicateFamilySymbolsDiagnostic();

        await Assert.That(symbols.Count).IsGreaterThan(0);
        foreach (FamilySymbol symbol in symbols)
        {
            DiagnosticFeedback feedback = diagnostic.Execute(document, null, symbol);

            await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
        }
    }

    [Test]
    public async Task Identical_nested_symbols_are_reported_in_family_document()
    {
        // A family document holds the nested families loaded into it as FamilySymbol
        // elements, so duplicates among them are reported just like in projects.
        Document host = FamilyTestDocuments.OpenModelFamilyTemplate(Application);
        try
        {
            Document nested = FamilyTestDocuments.OpenModelFamilyTemplate(Application);
            try
            {
                using (Transaction transaction = new(nested, "Create nested test family types"))
                {
                    transaction.Start();
                    FamilyTestDocuments.AddTextTypeParameter(nested, "Note");
                    nested.FamilyManager.NewType("LinterNestedA");
                    nested.FamilyManager.NewType("LinterNestedB");
                    transaction.Commit();
                }
                _ = nested.LoadFamily(host);
            }
            finally
            {
                nested.Close(false);
            }
            FamilySymbol first = FindSymbol(host, "LinterNestedA");
            FamilySymbol second = FindSymbol(host, "LinterNestedB");
            var diagnostic = new DuplicateFamilySymbolsDiagnostic();

            DiagnosticFeedback firstFeedback = diagnostic.Execute(host, null, first);
            DiagnosticFeedback secondFeedback = diagnostic.Execute(host, null, second);

            await Assert.That(firstFeedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
            await Assert.That(secondFeedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
            await Assert.That((string)firstFeedback.AdditionalMessageArguments!["duplicateNames"])
                .IsEqualTo("LinterNestedB");
            await Assert.That((string)secondFeedback.AdditionalMessageArguments!["duplicateNames"])
                .IsEqualTo("LinterNestedA");
        }
        finally
        {
            host.Close(false);
        }
    }

    [Test]
    public async Task Only_symbols_are_checked_by_filter()
    {
        Document document = _document!;
        Family family = FamilyTestDocuments.LoadFamilyWithType(Application, document, "LinterT1");
        FamilySymbol symbol = FindSymbol(document, "LinterT1");
        var filter = new DuplicateFamilySymbolsDiagnosticFilter();

        await Assert.That(filter.IsRelevantFor(document, symbol)).IsTrue();
        await Assert.That(filter.IsRelevantFor(document, family)).IsFalse();
    }

    [Test]
    public async Task Check_runs_in_any_document()
    {
        // Unlike project-only checks, nested families in a family document are
        // FamilySymbol elements too, so the check runs there as well.
        var filter = new DuplicateFamilySymbolsDiagnosticDocumentFilter();
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

    private static void LoadFamilyWithTypes(
        Document projectDocument, string parameterName, params string[] typeNames)
    {
        Document familyDocument = FamilyTestDocuments.OpenModelFamilyTemplate(
            projectDocument.Application);
        try
        {
            using (Transaction transaction = new(familyDocument, "Create test family types"))
            {
                transaction.Start();
                FamilyTestDocuments.AddTextTypeParameter(familyDocument, parameterName);
                foreach (string typeName in typeNames)
                    familyDocument.FamilyManager.NewType(typeName);
                transaction.Commit();
            }
            familyDocument.LoadFamily(projectDocument);
        }
        finally
        {
            familyDocument.Close(false);
        }
    }

    private static FamilySymbol FindSymbol(Document document, string typeName) =>
        new FilteredElementCollector(document)
            .OfClass(typeof(FamilySymbol))
            .Cast<FamilySymbol>()
            .First(candidate => candidate.Name == typeName);
}
