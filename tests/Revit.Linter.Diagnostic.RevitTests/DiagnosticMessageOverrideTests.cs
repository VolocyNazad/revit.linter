using Autodesk.Revit.DB;
using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Diagnostic.Abstractions.Services;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Models;

namespace Revit.Linter.Diagnostic.RevitTests;

/// <summary>
/// Covers the per-finding message format override: a diagnostic that returns its own format for an
/// uncertain result must reach the report with that format, while a finding without an override keeps
/// the message of the rule.
/// </summary>
public sealed class DiagnosticMessageOverrideTests : DiagnosticTestBase
{
    [Test]
    public async Task Feedback_message_format_overrides_the_diagnostic_message()
    {
        Element element = CreateLevel();
        ElementDiagnosticId id = CreateElementId("ELM-MSG", "Rule message: {elementName}");
        ElementDiagnostic diagnostic = new(
            id,
            new DiagnosticFeedback(DiagnosticVerdict.NotValid) { MessageFormat = "Unverified: {elementName}" });
        ReportSender sender = new();
        using ServiceProvider services = CreateServices(
            sender, elementDiagnostics: [CreateElementRegistration(id, diagnostic)]);

        services.GetRequiredService<IDiagnosticService>().Execute(_document!, [element.Id]);

        DiagnosticReport report = sender.Reports.Single();
        await Assert.That(report.Message.Format).IsEqualTo("Unverified: {elementName}");
        await Assert.That(GetArgument(report, "elementName")).IsEqualTo(element.Name);
    }

    [Test]
    public async Task Finding_without_override_uses_the_diagnostic_message()
    {
        Element element = CreateLevel();
        ElementDiagnosticId id = CreateElementId("ELM-MSG", "Rule message: {elementName}");
        ElementDiagnostic diagnostic = new(id, new DiagnosticFeedback(DiagnosticVerdict.NotValid));
        ReportSender sender = new();
        using ServiceProvider services = CreateServices(
            sender, elementDiagnostics: [CreateElementRegistration(id, diagnostic)]);

        services.GetRequiredService<IDiagnosticService>().Execute(_document!, [element.Id]);

        DiagnosticReport report = sender.Reports.Single();
        await Assert.That(report.Message.Format).IsEqualTo("Rule message: {elementName}");
    }
}
