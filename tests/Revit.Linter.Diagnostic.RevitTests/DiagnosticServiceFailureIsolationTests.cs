using Autodesk.Revit.DB;
using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.Diagnostic.Abstractions.Services;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Models;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed partial class DiagnosticServiceTests
{
    [Test]
    public async Task Failing_document_diagnostic_is_reported_and_does_not_stop_the_others()
    {
        DocumentDiagnosticId failingId = new(
            "DOC-FAIL", "Description", "Message", DiagnosticSeverity.Message,
            true, false, "");
        DocumentDiagnosticId workingId = new(
            "DOC-OK", "Description", "Message", DiagnosticSeverity.Warning,
            true, false, "");
        DocumentDiagnostic working = new(workingId, new DiagnosticFeedback(DiagnosticVerdict.NotValid));
        ReportSender sender = new();
        using ServiceProvider services = CreateServices(sender, collection =>
            collection.AddSingleton<IDiagnosticRegistrationProvider>(new TestRegistrationProvider(
                documentDiagnostics:
                [
                    new(failingId, new ThrowingDocumentDiagnostic(failingId, "Boom"),
                        new DocumentFilter(failingId, true),
                        CreateOverride(failingId, DiagnosticSeverity.Message, true), []),
                    new(workingId, working, new DocumentFilter(workingId, true),
                        CreateOverride(workingId, DiagnosticSeverity.Warning, true), [])
                ])));

        DiagnosticServiceResult result = services.GetRequiredService<IDiagnosticService>().Execute(_document!);

        await Assert.That(result).IsEqualTo(DiagnosticServiceResult.Failed);
        await Assert.That(working.ExecutionCount).IsEqualTo(1);
        await Assert.That(sender.Reports.Count).IsEqualTo(2);

        DiagnosticReport failure = sender.Reports.Single(report => report.Code == "DOC-FAIL");
        await Assert.That(failure.Severity).IsEqualTo(DiagnosticSeverity.Error);
        await Assert.That(failure.Target).IsEqualTo(_document);
        await Assert.That(GetArgument(failure, "error")).IsEqualTo("Boom");

        DiagnosticReport finding = sender.Reports.Single(report => report.Code == "DOC-OK");
        await Assert.That(finding.Severity).IsEqualTo(DiagnosticSeverity.Warning);
    }

    [Test]
    public async Task Failing_element_diagnostic_stops_at_first_failure_and_does_not_stop_the_others()
    {
        Element first = CreateLevelAt(0);
        Element second = CreateLevelAt(10);
        ElementDiagnosticId failingId = new(
            "ELM-FAIL", "Description", "Message", DiagnosticSeverity.Warning,
            true, false, "");
        ElementDiagnosticId workingId = new(
            "ELM-OK", "Description", "Message", DiagnosticSeverity.Warning,
            true, false, "");
        ThrowingElementDiagnostic failing = new(failingId, "Boom");
        ElementDiagnostic working = new(workingId, new DiagnosticFeedback(DiagnosticVerdict.NotValid));
        ReportSender sender = new();
        using ServiceProvider services = CreateServices(sender, collection =>
            collection.AddSingleton<IDiagnosticRegistrationProvider>(new TestRegistrationProvider(
                elementDiagnostics:
                [
                    new(failingId, failing, new ElementFilter(failingId, true),
                        new ElementDocumentFilter(failingId, true),
                        CreateOverride(failingId, DiagnosticSeverity.Warning, true), [], []),
                    new(workingId, working, new ElementFilter(workingId, true),
                        new ElementDocumentFilter(workingId, true),
                        CreateOverride(workingId, DiagnosticSeverity.Warning, true), [], [])
                ])));

        DiagnosticServiceResult result = services.GetRequiredService<IDiagnosticService>()
            .Execute(_document!, [first.Id, second.Id]);

        await Assert.That(result).IsEqualTo(DiagnosticServiceResult.Failed);
        await Assert.That(failing.ExecutionCount).IsEqualTo(1);
        await Assert.That(working.ExecutionCount).IsEqualTo(2);

        DiagnosticReport failure = sender.Reports.Single(report => report.Code == "ELM-FAIL");
        await Assert.That(failure.Severity).IsEqualTo(DiagnosticSeverity.Error);
        await Assert.That(failure.Target).IsEqualTo(_document);
        await Assert.That(GetArgument(failure, "error")).IsEqualTo("Boom");
        await Assert.That(sender.Reports.Count(report => report.Code == "ELM-OK")).IsEqualTo(2);
    }

    private Element CreateLevelAt(double elevation)
    {
        using Transaction transaction = new(_document!, "Create level");
        transaction.Start();
        Level level = Level.Create(_document!, elevation);
        transaction.Commit();
        return level;
    }

    private sealed class ThrowingDocumentDiagnostic(DocumentDiagnosticId identity, string message)
        : IDocumentDiagnostic
    {
        public DocumentDiagnosticId Identity { get; } = identity;
        public IEnumerable<DiagnosticFeedback> Execute(Document targetDocument) =>
            throw new InvalidOperationException(message);
    }

    private sealed class ThrowingElementDiagnostic(ElementDiagnosticId identity, string message)
        : IElementDiagnostic
    {
        public ElementDiagnosticId Identity { get; } = identity;
        public int ExecutionCount { get; private set; }
        public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
        {
            ExecutionCount++;
            throw new InvalidOperationException(message);
        }
    }
}
