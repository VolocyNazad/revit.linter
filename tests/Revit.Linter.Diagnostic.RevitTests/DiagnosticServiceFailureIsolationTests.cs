using Autodesk.Revit.DB;
using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.Diagnostic.Abstractions.Services;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Models;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class DiagnosticServiceFailureIsolationTests : DiagnosticTestBase
{
    [Test]
    public async Task Failing_document_diagnostic_is_reported_and_does_not_stop_the_others()
    {
        DocumentDiagnosticId failingId = CreateDocumentId("DOC-FAIL");
        DocumentDiagnosticId workingId = CreateDocumentId("DOC-OK");
        DocumentDiagnostic working = new(workingId, new DiagnosticFeedback(DiagnosticVerdict.NotValid));
        ReportSender sender = new();
        using ServiceProvider services = CreateServices(sender, documentDiagnostics:
        [
            CreateDocumentRegistration(
                failingId, new ThrowingDocumentDiagnostic(failingId, "Boom"), severity: DiagnosticSeverity.Message),
            CreateDocumentRegistration(workingId, working)
        ]);

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
        Element first = CreateLevel();
        Element second = CreateLevel(elevation: 10);
        ElementDiagnosticId failingId = CreateElementId("ELM-FAIL");
        ElementDiagnosticId workingId = CreateElementId("ELM-OK");
        ThrowingElementDiagnostic failing = new(failingId, "Boom");
        ElementDiagnostic working = new(workingId, new DiagnosticFeedback(DiagnosticVerdict.NotValid));
        ReportSender sender = new();
        using ServiceProvider services = CreateServices(sender, elementDiagnostics:
        [
            CreateElementRegistration(failingId, failing),
            CreateElementRegistration(workingId, working)
        ]);

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
