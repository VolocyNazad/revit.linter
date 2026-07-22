using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.FixReportProvider.Abstractions.Models;
using Revit.Linter.FixReportProvider.Abstractions.Services;
using Revit.Linter.FixReportProvider.DI;

namespace Revit.Linter.FixReportProvider.Tests;

public sealed class FixReportProviderTests
{
    [Fact]
    public void Send_notifies_subscriber_with_same_report_and_provider()
    {
        using ServiceProvider provider = CreateProvider();
        IFixReportReceiver receiver = provider.GetRequiredService<IFixReportReceiver>();
        IFixReportSender sender = provider.GetRequiredService<IFixReportSender>();
        FixReport report = CreateReport();
        object? eventSender = null;
        FixReport? received = null;
        receiver.ReportSent += (senderObject, args) => (eventSender, received) = (senderObject, args.Report);

        sender.Send(report);

        Assert.Same(report, received);
        Assert.Same(sender, eventSender);
    }

    [Fact]
    public void Send_notifies_each_subscriber_once()
    {
        using ServiceProvider provider = CreateProvider();
        IFixReportReceiver receiver = provider.GetRequiredService<IFixReportReceiver>();
        IFixReportSender sender = provider.GetRequiredService<IFixReportSender>();
        int firstCalls = 0;
        int secondCalls = 0;
        receiver.ReportSent += (_, _) => firstCalls++;
        receiver.ReportSent += (_, _) => secondCalls++;

        sender.Send(CreateReport());

        Assert.Equal(1, firstCalls);
        Assert.Equal(1, secondCalls);
    }

    [Fact]
    public void Unsubscribed_handler_is_not_called()
    {
        using ServiceProvider provider = CreateProvider();
        IFixReportReceiver receiver = provider.GetRequiredService<IFixReportReceiver>();
        IFixReportSender sender = provider.GetRequiredService<IFixReportSender>();
        int calls = 0;
        FixReportHandler handler = (_, _) => calls++;
        receiver.ReportSent += handler;
        receiver.ReportSent -= handler;

        sender.Send(CreateReport());

        Assert.Equal(0, calls);
    }

    [Fact]
    public void Send_without_subscribers_does_not_throw()
    {
        using ServiceProvider provider = CreateProvider();
        IFixReportSender sender = provider.GetRequiredService<IFixReportSender>();

        Exception? exception = Record.Exception(() => sender.Send(CreateReport()));

        Assert.Null(exception);
    }

    [Fact]
    public void Dependency_injection_resolves_sender_and_receiver_as_same_singleton()
    {
        ServiceCollection services = new();
        services.AddFixReportProviderModule();
        using ServiceProvider provider = services.BuildServiceProvider();

        object receiver = provider.GetRequiredService<IFixReportReceiver>();
        object sender = provider.GetRequiredService<IFixReportSender>();

        Assert.Same(receiver, sender);
        Assert.Same(sender, provider.GetRequiredService<IFixReportSender>());
    }

    private static ServiceProvider CreateProvider()
    {
        ServiceCollection services = new();
        services.AddFixReportProviderModule();
        return services.BuildServiceProvider();
    }

    private static FixReport CreateReport() => new(
        "TEST-001",
        "Test document",
        new FixReportMessage("Test message"));
}
