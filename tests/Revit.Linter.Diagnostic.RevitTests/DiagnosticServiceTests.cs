using Autodesk.Revit.DB;
using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Diagnostic.Abstractions.Services;
using Revit.Linter.Diagnostic.Infrastructure.Exceptions;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Models;
using Revit.Linter.ElementIgnoring.Abstractions.Services;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class DiagnosticServiceTests : DiagnosticTestBase
{
    [Test]
    public async Task Dependency_injection_registers_service_as_singleton()
    {
        using ServiceProvider services = CreateServices();

        IDiagnosticService first = services.GetRequiredService<IDiagnosticService>();
        IDiagnosticService second = services.GetRequiredService<IDiagnosticService>();

        await Assert.That(ReferenceEquals(first, second)).IsTrue();
    }

    [Test]
    public async Task Execute_succeeds_when_no_diagnostics_are_registered()
    {
        ReportSender sender = new();
        using ServiceProvider services = CreateServices(sender);

        DiagnosticServiceResult result = services.GetRequiredService<IDiagnosticService>().Execute(_document!);

        await Assert.That(result).IsEqualTo(DiagnosticServiceResult.Success);
        await Assert.That(sender.Reports).IsEmpty();
    }

    [Test]
    public async Task Execute_enumerates_requested_element_ids_once()
    {
        Element element = CreateLevel();
        int enumerationCount = 0;
        IEnumerable<ElementId> ElementIds()
        {
            enumerationCount++;
            yield return element.Id;
        }

        using ServiceProvider services = CreateServices(elementDiagnostics:
        [
            CreateElementRegistration(CreateElementId("ELM-ONE")),
            CreateElementRegistration(CreateElementId("ELM-TWO"))
        ]);

        DiagnosticServiceResult result = services.GetRequiredService<IDiagnosticService>()
            .Execute(_document!, ElementIds());

        await Assert.That(result).IsEqualTo(DiagnosticServiceResult.Success);
        await Assert.That(enumerationCount).IsEqualTo(1);
    }

    [Test]
    public async Task Document_diagnostic_sends_report_with_feedback_arguments()
    {
        DocumentDiagnosticId id = CreateDocumentId(
            "DOC001", "Value: {value}", isObsolete: true, obsoleteDescription: "Obsolete");
        DocumentDiagnostic diagnostic = new(id, new DiagnosticFeedback(
            DiagnosticVerdict.NotValid,
            new Dictionary<string, object> { ["value"] = 42 }));
        ReportSender sender = new();
        using ServiceProvider services = CreateServices(sender, documentDiagnostics:
            [CreateDocumentRegistration(id, diagnostic, severity: DiagnosticSeverity.Error)]);

        DiagnosticServiceResult result = services.GetRequiredService<IDiagnosticService>().Execute(_document!);

        await Assert.That(result).IsEqualTo(DiagnosticServiceResult.Success);
        await Assert.That(sender.Reports.Count).IsEqualTo(1);
        DiagnosticReport report = sender.Reports[0];
        await Assert.That(report.Code).IsEqualTo("DOC001");
        await Assert.That(report.Severity).IsEqualTo(DiagnosticSeverity.Error);
        await Assert.That(report.Target).IsEqualTo(_document);
        await Assert.That(report.IsObsolete).IsTrue();
        await Assert.That(report.ObsoleteDescription).IsEqualTo("Obsolete");
        await Assert.That(GetArgument(report, "documentTitle")).IsEqualTo(_document!.Title);
        await Assert.That(GetArgument(report, "value")).IsEqualTo(42);
    }

    [Test]
    public async Task Document_diagnostic_sends_one_report_per_invalid_feedback()
    {
        DocumentDiagnosticId id = CreateDocumentId("DOC-MULTI", "Value: {value}");
        DocumentDiagnostic diagnostic = new(
            id,
            new(DiagnosticVerdict.NotValid, new() { ["value"] = 1 }),
            DiagnosticFeedback.Valid,
            new(DiagnosticVerdict.NotValid, new() { ["value"] = 2 }));
        ReportSender sender = new();
        using ServiceProvider services = CreateServices(sender, documentDiagnostics:
            [CreateDocumentRegistration(id, diagnostic)]);

        DiagnosticServiceResult result = services.GetRequiredService<IDiagnosticService>().Execute(_document!);

        await Assert.That(result).IsEqualTo(DiagnosticServiceResult.Success);
        await Assert.That(sender.Reports.Count).IsEqualTo(2);
        await Assert.That(GetArgument(sender.Reports[0], "value")).IsEqualTo(1);
        await Assert.That(GetArgument(sender.Reports[1], "value")).IsEqualTo(2);
    }

    [Test]
    public async Task Inactive_document_diagnostic_is_not_executed()
    {
        DocumentDiagnosticId id = CreateDocumentId("DOC002");
        DocumentDiagnostic diagnostic = new(id, new DiagnosticFeedback(DiagnosticVerdict.NotValid));
        ReportSender sender = new();
        using ServiceProvider services = CreateServices(sender, documentDiagnostics:
            [CreateDocumentRegistration(id, diagnostic, isActive: false)]);

        DiagnosticServiceResult result = services.GetRequiredService<IDiagnosticService>().Execute(_document!);

        await Assert.That(result).IsEqualTo(DiagnosticServiceResult.Success);
        await Assert.That(diagnostic.ExecutionCount).IsEqualTo(0);
        await Assert.That(sender.Reports).IsEmpty();
    }

    [Test]
    public async Task Duplicate_document_diagnostic_identity_is_rejected_by_catalog()
    {
        DocumentDiagnosticId id = CreateDocumentId("DOC003");
        ReportSender sender = new();
        using ServiceProvider services = CreateServices(sender, documentDiagnostics:
            [CreateDocumentRegistration(id), CreateDocumentRegistration(id)]);

        Exception? exception = CaptureException(
            () => services.GetRequiredService<IDiagnosticService>());

        await Assert.That(exception).IsTypeOf<DuplicateDiagnosticIdException>();
        await Assert.That(sender.Reports).IsEmpty();
    }

    [Test]
    public async Task Element_diagnostic_sends_report_for_requested_element()
    {
        Element element = CreateLevel();
        ElementDiagnosticId id = CreateElementId("ELM001", "Value: {value}");
        ElementDiagnostic diagnostic = new(id, new(
            DiagnosticVerdict.NotValid,
            new Dictionary<string, object> { ["value"] = "test" },
            "dependency"));
        ReportSender sender = new();
        using ServiceProvider services = CreateServices(sender, elementDiagnostics:
            [CreateElementRegistration(id, diagnostic)]);

        DiagnosticServiceResult result = services.GetRequiredService<IDiagnosticService>()
            .Execute(_document!, [element.Id]);

        await Assert.That(result).IsEqualTo(DiagnosticServiceResult.Success);
        await Assert.That(sender.Reports.Count).IsEqualTo(1);
        DiagnosticReport report = sender.Reports[0];
        await Assert.That(report.Code).IsEqualTo("ELM001");
        await Assert.That(((Element)report.Target!).Id == element.Id).IsTrue();
        await Assert.That(report.TargetDependencies).IsNotNull();
        await Assert.That(report.TargetDependencies!.Length).IsEqualTo(1);
        await Assert.That(report.TargetDependencies[0]).IsEqualTo("dependency");
        await Assert.That(GetArgument(report, "elementId")).IsEqualTo(element.Id);
        await Assert.That(GetArgument(report, "elementName")).IsEqualTo(element.Name);
        await Assert.That(GetArgument(report, "value")).IsEqualTo("test");
    }

    [Test]
    public async Task Ignored_element_is_not_diagnosed()
    {
        Element element = CreateLevel();
        ElementDiagnosticId id = CreateElementId("ELM002");
        ElementDiagnostic diagnostic = new(id, new(DiagnosticVerdict.NotValid));
        ReportSender sender = new();
        using ServiceProvider services = CreateServices(
            sender,
            collection => collection.AddSingleton<IIgnoreElementDetector>(new IgnoreElementDetector(true)),
            elementDiagnostics: [CreateElementRegistration(id, diagnostic)]);

        DiagnosticServiceResult result = services.GetRequiredService<IDiagnosticService>()
            .Execute(_document!, [element.Id]);

        await Assert.That(result).IsEqualTo(DiagnosticServiceResult.Success);
        await Assert.That(diagnostic.ExecutionCount).IsEqualTo(0);
        await Assert.That(sender.Reports).IsEmpty();
    }
}
