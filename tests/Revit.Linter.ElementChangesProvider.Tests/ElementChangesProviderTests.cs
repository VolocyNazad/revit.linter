using Autodesk.Revit.DB;
using Microsoft.Extensions.DependencyInjection;
using Nice3point.TUnit.Revit;
using Revit.Linter.ElementChangesProvider.Abstractions.Models;
using Revit.Linter.ElementChangesProvider.Abstractions.Services;
using Revit.Linter.ElementChangesProvider.DI;

namespace Revit.Linter.ElementChangesProvider.Tests;

public sealed class ElementChangesProviderTests : RevitApiTest
{
    [Test]
    public async Task Send_notifies_subscriber_with_same_changes_and_provider()
    {
        using ServiceProvider provider = CreateProvider();
        IElementChangesReceiver receiver = provider.GetRequiredService<IElementChangesReceiver>();
        IElementChangesSender sender = provider.GetRequiredService<IElementChangesSender>();
        ElementChanges changes = CreateChanges();
        object? eventSender = null;
        ElementChanges? received = null;
        receiver.Sent += (senderObject, args) => (eventSender, received) = (senderObject, args.Changes);

        sender.Send(changes);

        await Assert.That(ReferenceEquals(changes, received)).IsTrue();
        await Assert.That(ReferenceEquals(sender, eventSender)).IsTrue();
    }

    [Test]
    public async Task Send_notifies_each_subscriber_once()
    {
        using ServiceProvider provider = CreateProvider();
        IElementChangesReceiver receiver = provider.GetRequiredService<IElementChangesReceiver>();
        IElementChangesSender sender = provider.GetRequiredService<IElementChangesSender>();
        int firstCalls = 0;
        int secondCalls = 0;
        receiver.Sent += (_, _) => firstCalls++;
        receiver.Sent += (_, _) => secondCalls++;

        sender.Send(CreateChanges());

        await Assert.That(firstCalls).IsEqualTo(1);
        await Assert.That(secondCalls).IsEqualTo(1);
    }

    [Test]
    public async Task Unsubscribed_handler_is_not_called()
    {
        using ServiceProvider provider = CreateProvider();
        IElementChangesReceiver receiver = provider.GetRequiredService<IElementChangesReceiver>();
        IElementChangesSender sender = provider.GetRequiredService<IElementChangesSender>();
        int calls = 0;
        ElementChangesHandler handler = (_, _) => calls++;
        receiver.Sent += handler;
        receiver.Sent -= handler;

        sender.Send(CreateChanges());

        await Assert.That(calls).IsEqualTo(0);
    }

    [Test]
    public void Send_without_subscribers_does_not_throw()
    {
        using ServiceProvider provider = CreateProvider();
        IElementChangesSender sender = provider.GetRequiredService<IElementChangesSender>();

        sender.Send(CreateChanges());
    }

    [Test]
    public async Task Dependency_injection_resolves_sender_and_receiver_as_same_singleton()
    {
        ServiceCollection services = new();
        services.AddElementChangesProviderModule();
        using ServiceProvider provider = services.BuildServiceProvider();

        object receiver = provider.GetRequiredService<IElementChangesReceiver>();
        object sender = provider.GetRequiredService<IElementChangesSender>();

        await Assert.That(ReferenceEquals(receiver, sender)).IsTrue();
        await Assert.That(ReferenceEquals(sender, provider.GetRequiredService<IElementChangesSender>())).IsTrue();
    }

    private static ServiceProvider CreateProvider()
    {
        ServiceCollection services = new();
        services.AddElementChangesProviderModule();
        return services.BuildServiceProvider();
    }

    private static ElementChanges CreateChanges() => new(
        null!, Array.Empty<ElementId>(), Array.Empty<ElementId>(), Array.Empty<ElementId>());
}
