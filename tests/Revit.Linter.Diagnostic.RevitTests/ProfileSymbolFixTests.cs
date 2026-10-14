using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.ElementDiagnostics.Diagnostics.ProfileFamilySymbolUnused;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class ProfileSymbolFixTests : RevitApiTest
{
    private Document? _document;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() => _document = Application.NewProjectDocument(UnitSystem.Metric);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _document?.Close(false);

    [Test]
    public async Task Delete_unused_profile_symbol_removes_symbol()
    {
        Document document = _document!;
        FamilySymbol symbol = LoadProfileSymbol(document);
        ElementId symbolId = symbol.Id;
        var fix = new DeleteUnusedProfileSymbol();

        bool result;
        using (Transaction transaction = new(document, "Delete test profile symbol"))
        {
            transaction.Start();
            result = fix.Execute(symbol);
            transaction.Commit();
        }

        await Assert.That(fix.Identity.Code).IsEqualTo("SHRD0023");
        await Assert.That(result).IsTrue();
        await Assert.That(document.GetElement(symbolId)).IsNull();
    }

    // Profile templates have locale-specific file names; both English and Russian stems are matched.
    private static FamilySymbol LoadProfileSymbol(Document document)
    {
        Autodesk.Revit.ApplicationServices.Application application = document.Application
            ?? throw new InvalidOperationException("The test document has no application.");
        string templatePath = Directory
            .EnumerateFiles(application.FamilyTemplatePath, "*.rft", SearchOption.AllDirectories)
            .FirstOrDefault(path =>
                Path.GetFileNameWithoutExtension(path).Contains("profil", StringComparison.OrdinalIgnoreCase)
                || Path.GetFileNameWithoutExtension(path).Contains("профил", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"No profile family template was found below '{application.FamilyTemplatePath}'.");
        Document familyDocument = application.NewFamilyDocument(templatePath);
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
}
