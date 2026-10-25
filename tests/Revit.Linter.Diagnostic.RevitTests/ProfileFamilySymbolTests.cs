using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.ElementDiagnostics.Diagnostics.ProfileFamilySymbolUnused;
using Revit.Linter.Testing;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class ProfileFamilySymbolTests : RevitApiTest
{
    private Document? _document;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() => _document = Application.NewProjectDocument(UnitSystem.Metric);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _document?.Close(false);

    [Test]
    public async Task Unused_profile_symbol_is_reported()
    {
        Document document = _document!;
        FamilySymbol symbol = LoadProfileSymbol(Application, document);
        var diagnostic = new ProfileFamilySymbolUnusedDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, symbol);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Only_profile_symbols_are_checked_by_filter()
    {
        // No True-case: a true Profiles symbol cannot be built through the API
        // (profile templates refuse sketch planes and model curves, empty profiles
        // refuse to load, and the Profiles category is missing from family documents).
        // That case needs a checked-in profile .rfa asset.
        Document document = _document!;
        Family family = FamilyTestDocuments.LoadFamilyWithType(Application, document, "LinterT1");
        FamilySymbol plainSymbol = FamilyTestDocuments.FindSymbolByTypeName(document, "LinterT1");
        var filter = new ProfileFamilySymbolUnusedDiagnosticFilter();

        await Assert.That(filter.IsRelevantFor(document, plainSymbol)).IsFalse();
        await Assert.That(filter.IsRelevantFor(document, family)).IsFalse();
    }

    [Test]
    public async Task Check_runs_in_projects_only()
    {
        var filter = new ProfileFamilySymbolUnusedDiagnosticDocumentFilter();
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

    private static FamilySymbol LoadProfileSymbol(
        Autodesk.Revit.ApplicationServices.Application application, Document document)
    {
        Document familyDocument = OpenProfileFamily(application);
        try
        {
            using (Transaction transaction = new(familyDocument, "Create test profile type"))
            {
                transaction.Start();
                familyDocument.FamilyManager.NewType("LinterProfile");
                transaction.Commit();
            }
            Family family = familyDocument.LoadFamily(document);
            return new FilteredElementCollector(document)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .First(candidate => candidate.Family.Id == family.Id);
        }
        finally
        {
            familyDocument.Close(false);
        }
    }

    // Profile templates have locale-specific file names; both English and Russian stems are matched.
    private static Document OpenProfileFamily(Autodesk.Revit.ApplicationServices.Application application)
    {
        string templatePath = Directory
            .EnumerateFiles(application.FamilyTemplatePath, "*.rft", SearchOption.AllDirectories)
            .FirstOrDefault(path =>
                Path.GetFileNameWithoutExtension(path).Contains("profil", StringComparison.OrdinalIgnoreCase)
                || Path.GetFileNameWithoutExtension(path).Contains("профил", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"No profile family template was found below '{application.FamilyTemplatePath}'.");
        return application.NewFamilyDocument(templatePath);
    }
}
