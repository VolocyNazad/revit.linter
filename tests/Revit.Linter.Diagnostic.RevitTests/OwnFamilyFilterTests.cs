using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.ElementDiagnostics.Diagnostics.FamilyTypeUnused;
using Revit.Linter.ElementDiagnostics.Diagnostics.FamilyUnused;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class OwnFamilyFilterTests : RevitApiTest
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
    public async Task Edited_family_is_excluded_from_unused_family_check()
    {
        Document document = _familyDocument!;
        Family owner = document.OwnerFamily!;
        var filter = new FamilyUnusedDiagnosticFilter();

        bool relevant = filter.IsRelevantFor(document, owner);

        await Assert.That(relevant).IsFalse();
    }

    [Test]
    public async Task Nested_symbols_are_checked_in_family_document()
    {
        // The edited family's own types live only in the FamilyManager, never as
        // FamilySymbol elements, so the own-symbol exclusion has no constructible
        // fixture: nested families' symbols prove symbols are still checked here.
        Document document = _familyDocument!;
        FamilySymbol nested = new FilteredElementCollector(document)
            .OfClass(typeof(FamilySymbol))
            .Cast<FamilySymbol>()
            .FirstOrDefault()
            ?? throw new InvalidOperationException("The test family nests no families.");
        var filter = new FamilySymbolUnusedDiagnosticFilter();

        await Assert.That(filter.IsRelevantFor(document, nested)).IsTrue();
    }

    [Test]
    public async Task Loaded_family_is_checked_in_project_document()
    {
        Document projectDocument = Application.NewProjectDocument(UnitSystem.Metric);
        try
        {
            Family family = FamilyTestDocuments.LoadFamilyWithType(Application, projectDocument, "LinterT1");
            var filter = new FamilyUnusedDiagnosticFilter();

            bool relevant = filter.IsRelevantFor(projectDocument, family);

            await Assert.That(relevant).IsTrue();
        }
        finally
        {
            projectDocument.Close(false);
        }
    }

    [Test]
    public async Task Loaded_symbol_is_checked_in_project_document()
    {
        Document projectDocument = Application.NewProjectDocument(UnitSystem.Metric);
        try
        {
            _ = FamilyTestDocuments.LoadFamilyWithType(Application, projectDocument, "LinterT1");
            FamilySymbol symbol = FamilyTestDocuments.FindSymbolByTypeName(projectDocument, "LinterT1");
            var filter = new FamilySymbolUnusedDiagnosticFilter();

            bool relevant = filter.IsRelevantFor(projectDocument, symbol);

            await Assert.That(relevant).IsTrue();
        }
        finally
        {
            projectDocument.Close(false);
        }
    }
}
