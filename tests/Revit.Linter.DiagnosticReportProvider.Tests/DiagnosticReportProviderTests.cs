using Microsoft.Extensions.DependencyInjection;
using Nice3point.TUnit.Revit;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Models;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Services;
using Revit.Linter.DiagnosticReportProvider.DI;

namespace Revit.Linter.DiagnosticReportProvider.Tests;

public sealed class DiagnosticReportProviderTests : RevitApiTest
{
    [Test]
    public async Task Send_notifies_subscriber_with_same_report_and_provider()
    {
        using ServiceProvider provider = CreateProvider();
        IDiagnosticReportReceiver receiver = provider.GetRequiredService<IDiagnosticReportReceiver>();
        IDiagnosticReportSender sender = provider.GetRequiredService<IDiagnosticReportSender>();
        DiagnosticReport report = CreateReport();
        object? eventSender = null;
        DiagnosticReport? received = null;
        receiver.ReportSent += (senderObject, args) => (eventSender, received) = (senderObject, args.Report);

        sender.Send(report);

        await Assert.That(ReferenceEquals(report, received)).IsTrue();
        await Assert.That(ReferenceEquals(sender, eventSender)).IsTrue();
    }

    [Test]
    public async Task Send_notifies_each_subscriber_once()
    {
        using ServiceProvider provider = CreateProvider();
        IDiagnosticReportReceiver receiver = provider.GetRequiredService<IDiagnosticReportReceiver>();
        IDiagnosticReportSender sender = provider.GetRequiredService<IDiagnosticReportSender>();
        int firstCalls = 0;
        int secondCalls = 0;
        receiver.ReportSent += (_, _) => firstCalls++;
        receiver.ReportSent += (_, _) => secondCalls++;

        sender.Send(CreateReport());

        await Assert.That(firstCalls).IsEqualTo(1);
        await Assert.That(secondCalls).IsEqualTo(1);
    }

    [Test]
    public async Task Unsubscribed_handler_is_not_called()
    {
        using ServiceProvider provider = CreateProvider();
        IDiagnosticReportReceiver receiver = provider.GetRequiredService<IDiagnosticReportReceiver>();
        IDiagnosticReportSender sender = provider.GetRequiredService<IDiagnosticReportSender>();
        int calls = 0;
        DiagnosticReportHandler handler = (_, _) => calls++;
        receiver.ReportSent += handler;
        receiver.ReportSent -= handler;

        sender.Send(CreateReport());

        await Assert.That(calls).IsEqualTo(0);
    }

    [Test]
    public void Send_without_subscribers_does_not_throw()
    {
        using ServiceProvider provider = CreateProvider();
        IDiagnosticReportSender sender = provider.GetRequiredService<IDiagnosticReportSender>();

        sender.Send(CreateReport());
    }

    [Test]
    public async Task Dependency_injection_resolves_sender_and_receiver_as_same_singleton()
    {
        ServiceCollection services = new();
        services.AddDiagnosticReportProviderModule();
        using ServiceProvider provider = services.BuildServiceProvider();

        object receiver = provider.GetRequiredService<IDiagnosticReportReceiver>();
        object sender = provider.GetRequiredService<IDiagnosticReportSender>();

        await Assert.That(ReferenceEquals(receiver, sender)).IsTrue();
        await Assert.That(ReferenceEquals(sender, provider.GetRequiredService<IDiagnosticReportSender>())).IsTrue();
    }

    private static ServiceProvider CreateProvider()
    {
        ServiceCollection services = new();
        services.AddDiagnosticReportProviderModule();
        return services.BuildServiceProvider();
    }

    private static DiagnosticReport CreateReport() => new(
        "TEST-001", DiagnosticSeverity.Warning, null!, new DiagnosticReportMessage("Test message"));
}
