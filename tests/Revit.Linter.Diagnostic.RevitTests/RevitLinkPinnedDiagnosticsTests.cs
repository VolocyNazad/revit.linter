using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.ElementDiagnostics.Diagnostics.RevitLinkNotPinned;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

/// <summary>
/// Covers the pinned Revit link diagnostic (SHRD0037): an unpinned loaded link is a finding, a pinned
/// link is not, the finding carries a fix that pins the link, and the check stays out of family
/// documents. The link instance is built from a real project document saved to a temporary file,
/// because the Revit API can link only an existing model.
/// </summary>
public sealed class RevitLinkPinnedDiagnosticsTests : RevitApiTest
{
    private Document? _document;
    private RevitLinkInstance? _link;
    private string? _linkPath;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument()
    {
        _document = Application.NewProjectDocument(UnitSystem.Metric);
        _linkPath = Path.Combine(Path.GetTempPath(), $"LinkPinnedDiagnostic{Guid.NewGuid():N}.rvt");

        Document linkDocument = Application.NewProjectDocument(UnitSystem.Metric);
        using (Transaction linkTransaction = new(linkDocument, "Seed linked pinned test document"))
        {
            linkTransaction.Start();
            Level.Create(linkDocument, 0.0);
            linkTransaction.Commit();
        }
        linkDocument.SaveAs(_linkPath);
        linkDocument.Close(false);

        using Transaction transaction = new(_document, "Link a document for pinned link tests");
        transaction.Start();
        LinkLoadResult linkResult = RevitLinkType.Create(
            _document,
            ModelPathUtils.ConvertUserVisiblePathToModelPath(_linkPath),
            new RevitLinkOptions(false));
        if (linkResult.ElementId == ElementId.InvalidElementId)
            throw new InvalidOperationException(
                $"The pinned link test document could not be linked: {linkResult.LoadResult}.");
        ElementId instanceId = RevitLinkInstance.Create(_document, linkResult.ElementId).Id;
        transaction.Commit();
        _link = (RevitLinkInstance)_document.GetElement(instanceId)
            ?? throw new InvalidOperationException("The linked test link instance was not found.");
    }

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument()
    {
        _document?.Close(false);
        if (_linkPath is not null && File.Exists(_linkPath)) File.Delete(_linkPath);
    }

    [Test]
    public async Task Unpinned_link_is_reported()
    {
        Document document = _document!;
        var diagnostic = new RevitLinkNotPinnedDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, _link!);

        await Assert.That(diagnostic.Identity.Code).IsEqualTo("SHRD0037");
        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Pinned_link_is_valid()
    {
        Document document = _document!;
        Pin(document, _link!);
        RevitLinkInstance pinnedLink = (RevitLinkInstance)document.GetElement(_link!.Id)
            ?? throw new InvalidOperationException("The test link instance was not found after pinning.");
        var diagnostic = new RevitLinkNotPinnedDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, pinnedLink);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Non_link_elements_are_not_checked()
    {
        Document document = _document!;
        Level level = CreateLevel(document);
        var filter = new RevitLinkNotPinnedDiagnosticFilter();
        var diagnostic = new RevitLinkNotPinnedDiagnostic();

        await Assert.That(filter.IsRelevantFor(document, _link!)).IsTrue();
        await Assert.That(filter.IsRelevantFor(document, level)).IsFalse();
        await Assert.That(diagnostic.Execute(document, null, level).Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Check_is_hidden_in_family_documents()
    {
        var documentFilter = new RevitLinkNotPinnedDiagnosticDocumentFilter();
        Document projectDocument = _document!;

        Document familyDocument = FamilyTestDocuments.OpenModelFamilyTemplate(Application);
        try
        {
            await Assert.That(documentFilter.IsRelevantFor(projectDocument)).IsTrue();
            await Assert.That(documentFilter.IsRelevantFor(familyDocument)).IsFalse();
        }
        finally
        {
            familyDocument.Close(false);
        }
    }

    [Test]
    public async Task Pin_link_fix_pins_the_link()
    {
        Document document = _document!;
        var fix = new PinRevitLink();

        bool result = ApplyFix(document, fix, _link!);

        RevitLinkInstance pinnedLink = (RevitLinkInstance)document.GetElement(_link!.Id)
            ?? throw new InvalidOperationException("The test link instance was not found after the fix.");
        await Assert.That(fix.Identity.Code).IsEqualTo("SHRD0037");
        await Assert.That(result).IsTrue();
        await Assert.That(pinnedLink.Pinned).IsTrue();
    }

    private static Level CreateLevel(Document document)
    {
        using Transaction transaction = new(document, "Create test level");
        transaction.Start();
        Level level = Level.Create(document, 0);
        transaction.Commit();
        return level;
    }

    private static void Pin(Document document, Element element)
    {
        using Transaction transaction = new(document, "Pin test element");
        transaction.Start();
        element.Pinned = true;
        transaction.Commit();
    }

    private static bool ApplyFix(Document document, IElementFix fix, Element element)
    {
        using Transaction transaction = new(document, "Apply test fix");
        transaction.Start();
        bool result = fix.Execute(element);
        transaction.Commit();
        return result;
    }
}
