using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.Diagnostic.DI;
using Revit.Linter.ElementDiagnostics.DI;
using Revit.Linter.ValueStore.Abstractions;
using Revit.TransactionMemoryCache.Abstractions.Services;

namespace Revit.Linter.Diagnostic.Tests;

public sealed partial class DiagnosticServiceTests
{
    [Test]
    public async Task Catalog_aggregates_registrations_and_fixes_from_providers()
    {
        ElementDiagnosticId elementId = new(
            "ELM-CATALOG", "Description", "Message", DiagnosticSeverity.Warning,
            true, false, "");
        DocumentDiagnosticId documentId = new(
            "DOC-CATALOG", "Description", "Message", DiagnosticSeverity.Warning,
            true, false, "");
        ElementFix elementFix = new(elementId);
        DocumentFix documentFix = new(documentId);
        ElementDiagnosticRegistration elementRegistration = new(
            elementId,
            new ElementDiagnostic(elementId, DiagnosticFeedback.Valid),
            new ElementFilter(elementId, true),
            new ElementDocumentFilter(elementId, true),
            CreateOverride(elementId, DiagnosticSeverity.Warning, true),
            [elementFix]);
        DocumentDiagnosticRegistration documentRegistration = new(
            documentId,
            new DocumentDiagnostic(documentId, DiagnosticFeedback.Valid),
            new DocumentFilter(documentId, true),
            CreateOverride(documentId, DiagnosticSeverity.Warning, true),
            [documentFix]);

        using ServiceProvider services = CreateServices(configure: collection =>
        {
            collection.AddSingleton<IDiagnosticRegistrationProvider>(
                new TestRegistrationProvider(elementDiagnostics: [elementRegistration]));
            collection.AddSingleton<IDiagnosticRegistrationProvider>(
                new TestRegistrationProvider(documentDiagnostics: [documentRegistration]));
        });

        IDiagnosticCatalog catalog = services.GetRequiredService<IDiagnosticCatalog>();
        using IDiagnosticCatalogSnapshotLease lease = catalog.AcquireSnapshot();
        using IDiagnosticCatalogSnapshotLease secondLease = catalog.AcquireSnapshot();
        DiagnosticCatalogSnapshot snapshot = lease.Snapshot;

        await Assert.That(ReferenceEquals(snapshot, secondLease.Snapshot)).IsTrue();
        await Assert.That(snapshot.ElementDiagnostics is ElementDiagnosticRegistration[]).IsFalse();
        await Assert.That(snapshot.DocumentDiagnostics is DocumentDiagnosticRegistration[]).IsFalse();
        await Assert.That(CaptureException(() =>
            ((IList<ElementDiagnosticRegistration>)snapshot.ElementDiagnostics).Add(elementRegistration)))
            .IsTypeOf<NotSupportedException>();
        await Assert.That(snapshot.ElementDiagnostics).Count().IsEqualTo(1);
        await Assert.That(snapshot.DocumentDiagnostics).Count().IsEqualTo(1);
        await Assert.That(ReferenceEquals(snapshot.ElementDiagnostics[0].Fixes.Single(), elementFix)).IsTrue();
        await Assert.That(ReferenceEquals(snapshot.DocumentDiagnostics[0].Fixes.Single(), documentFix)).IsTrue();
    }

    [Test]
    public async Task Disposing_catalog_disposes_owned_components()
    {
        ElementDiagnosticId id = new(
            "ELM-DISPOSE", "Description", "Message", DiagnosticSeverity.Warning,
            true, false, "");
        TrackingValueStore<ElementDiagnosticOverridesSettings> store = new(new());
        ElementFix fix = new(id);
        ElementDiagnosticIdOverride diagnosticOverride = new(id, store);
        ElementDiagnosticRegistration registration = new(
            id,
            new ElementDiagnostic(id, DiagnosticFeedback.Valid),
            new ElementFilter(id, true),
            new ElementDocumentFilter(id, true),
            diagnosticOverride,
            [fix]);
        ServiceProvider services = CreateServices(configure: collection =>
            collection.AddSingleton<IDiagnosticRegistrationProvider>(
                new TestRegistrationProvider(elementDiagnostics: [registration])));
        _ = services.GetRequiredService<IDiagnosticCatalog>();

        await services.DisposeAsync();

        await Assert.That(store.SubscriptionDisposed).IsTrue();
        await Assert.That(fix.IsDisposed).IsTrue();
    }

    [Test]
    public async Task Active_lease_defers_snapshot_disposal()
    {
        ElementDiagnosticId id = new(
            "ELM-LEASE", "Description", "Message", DiagnosticSeverity.Warning,
            true, false, "");
        TrackingValueStore<ElementDiagnosticOverridesSettings> store = new(new());
        ElementFix fix = new(id);
        ElementDiagnosticRegistration registration = new(
            id,
            new ElementDiagnostic(id, DiagnosticFeedback.Valid),
            new ElementFilter(id, true),
            new ElementDocumentFilter(id, true),
            new ElementDiagnosticIdOverride(id, store),
            [fix]);
        ServiceProvider services = CreateServices(configure: collection =>
            collection.AddSingleton<IDiagnosticRegistrationProvider>(
                new TestRegistrationProvider(elementDiagnostics: [registration])));
        IDiagnosticCatalogSnapshotLease lease = services
            .GetRequiredService<IDiagnosticCatalog>()
            .AcquireSnapshot();

        await services.DisposeAsync();

        await Assert.That(store.SubscriptionDisposed).IsFalse();
        await Assert.That(fix.IsDisposed).IsFalse();

        lease.Dispose();

        await Assert.That(store.SubscriptionDisposed).IsTrue();
        await Assert.That(fix.IsDisposed).IsTrue();
    }

    [Test]
    public async Task Refresh_publishes_new_version_and_retires_previous_snapshot()
    {
        RefreshingRegistrationProvider provider = new();
        using ServiceProvider services = CreateServices(configure: collection =>
            collection.AddSingleton<IDiagnosticRegistrationProvider>(provider));
        IDiagnosticCatalog catalog = services.GetRequiredService<IDiagnosticCatalog>();
        using IDiagnosticCatalogSnapshotLease oldLease = catalog.AcquireSnapshot();
        long? changedVersion = null;
        DiagnosticCatalogChangeOrigin? changeOrigin = null;
        catalog.Changed += (_, args) =>
        {
            changedVersion = args.Version;
            changeOrigin = args.Origin;
        };

        catalog.Refresh();
        using IDiagnosticCatalogSnapshotLease newLease = catalog.AcquireSnapshot();

        await Assert.That(oldLease.Version).IsEqualTo(1);
        await Assert.That(newLease.Version).IsEqualTo(2);
        await Assert.That(changedVersion).IsEqualTo(2);
        await Assert.That(changeOrigin).IsEqualTo(DiagnosticCatalogChangeOrigin.Manual);
        await Assert.That(ReferenceEquals(oldLease.Snapshot, newLease.Snapshot)).IsFalse();
        await Assert.That(provider.Fixes[0].IsDisposed).IsFalse();

        oldLease.Dispose();

        await Assert.That(provider.Fixes[0].IsDisposed).IsTrue();
        await Assert.That(provider.Fixes[1].IsDisposed).IsFalse();
    }

    [Test]
    public async Task Failed_refresh_keeps_current_snapshot_and_does_not_raise_changed()
    {
        RefreshingRegistrationProvider provider = new();
        using ServiceProvider services = CreateServices(configure: collection =>
            collection.AddSingleton<IDiagnosticRegistrationProvider>(provider));
        IDiagnosticCatalog catalog = services.GetRequiredService<IDiagnosticCatalog>();
        using IDiagnosticCatalogSnapshotLease before = catalog.AcquireSnapshot();
        bool changed = false;
        catalog.Changed += (_, _) => changed = true;
        provider.ThrowOnCreate = true;

        Exception? exception = CaptureException(catalog.Refresh);
        using IDiagnosticCatalogSnapshotLease after = catalog.AcquireSnapshot();

        await Assert.That(exception).IsTypeOf<InvalidOperationException>();
        await Assert.That(changed).IsFalse();
        await Assert.That(after.Version).IsEqualTo(before.Version);
        await Assert.That(ReferenceEquals(after.Snapshot, before.Snapshot)).IsTrue();
    }

    [Test]
    public async Task Change_notifications_are_debounced_into_one_refresh()
    {
        RefreshingRegistrationProvider provider = new();
        TestCatalogChangeSource changeSource = new();
        using ServiceProvider services = CreateServices(configure: collection =>
        {
            collection.AddSingleton<IDiagnosticRegistrationProvider>(provider);
            collection.AddSingleton<IDiagnosticCatalogChangeSource>(changeSource);
        });
        IDiagnosticCatalog catalog = services.GetRequiredService<IDiagnosticCatalog>();
        using IDiagnosticCatalogSnapshotLease initial = catalog.AcquireSnapshot();
        DiagnosticCatalogChangeOrigin? changeOrigin = null;
        catalog.Changed += (_, args) => changeOrigin = args.Origin;

        changeSource.Notify();
        changeSource.Notify();
        changeSource.Notify();
        await Task.Delay(700);
        using IDiagnosticCatalogSnapshotLease refreshed = catalog.AcquireSnapshot();

        await Assert.That(initial.Version).IsEqualTo(1);
        await Assert.That(refreshed.Version).IsEqualTo(2);
        await Assert.That(changeOrigin).IsEqualTo(DiagnosticCatalogChangeOrigin.ExternalFile);
    }

    [Test]
    public async Task Automatic_refresh_keeps_snapshot_on_failure_and_recovers_on_next_change()
    {
        RefreshingRegistrationProvider provider = new();
        TestCatalogChangeSource changeSource = new();
        using ServiceProvider services = CreateServices(configure: collection =>
        {
            collection.AddSingleton<IDiagnosticRegistrationProvider>(provider);
            collection.AddSingleton<IDiagnosticCatalogChangeSource>(changeSource);
        });
        IDiagnosticCatalog catalog = services.GetRequiredService<IDiagnosticCatalog>();
        using IDiagnosticCatalogSnapshotLease initial = catalog.AcquireSnapshot();
        Exception? refreshError = null;
        int refreshFailureCount = 0;
        catalog.RefreshFailed += (_, args) =>
        {
            refreshError = args.Exception;
            refreshFailureCount++;
        };
        provider.ThrowOnCreate = true;

        changeSource.Notify();
        await Task.Delay(700);
        using IDiagnosticCatalogSnapshotLease afterFailure = catalog.AcquireSnapshot();

        provider.ThrowOnCreate = false;
        changeSource.Notify();
        await Task.Delay(700);
        using IDiagnosticCatalogSnapshotLease recovered = catalog.AcquireSnapshot();

        await Assert.That(afterFailure.Version).IsEqualTo(initial.Version);
        await Assert.That(ReferenceEquals(afterFailure.Snapshot, initial.Snapshot)).IsTrue();
        await Assert.That(refreshFailureCount).IsEqualTo(1);
        await Assert.That(refreshError).IsTypeOf<InvalidOperationException>();
        await Assert.That(recovered.Version).IsEqualTo(2);
        await Assert.That(ReferenceEquals(recovered.Snapshot, initial.Snapshot)).IsFalse();
    }

    [Test]
    public async Task Disposed_catalog_rejects_new_operations_but_active_lease_remains_valid()
    {
        ServiceProvider services = CreateServices();
        IDiagnosticCatalog catalog = services.GetRequiredService<IDiagnosticCatalog>();
        IDiagnosticCatalogSnapshotLease lease = catalog.AcquireSnapshot();

        await services.DisposeAsync();

        await Assert.That(CaptureException(() => catalog.AcquireSnapshot()))
            .IsTypeOf<ObjectDisposedException>();
        await Assert.That(CaptureException(catalog.Refresh))
            .IsTypeOf<ObjectDisposedException>();
        await Assert.That(lease.Snapshot).IsNotNull();

        lease.Dispose();
        lease.Dispose();
    }

    [Test]
    public async Task Disposing_catalog_unsubscribes_from_change_sources()
    {
        TestCatalogChangeSource changeSource = new();
        ServiceProvider services = CreateServices(configure: collection =>
            collection.AddSingleton<IDiagnosticCatalogChangeSource>(changeSource));
        _ = services.GetRequiredService<IDiagnosticCatalog>();
        await Assert.That(changeSource.ListenerCount).IsEqualTo(1);

        await services.DisposeAsync();

        await Assert.That(changeSource.ListenerCount).IsEqualTo(0);
    }

    [Test]
    public async Task Failing_changed_handler_does_not_block_other_handlers()
    {
        RefreshingRegistrationProvider provider = new();
        using ServiceProvider services = CreateServices(configure: collection =>
            collection.AddSingleton<IDiagnosticRegistrationProvider>(provider));
        IDiagnosticCatalog catalog = services.GetRequiredService<IDiagnosticCatalog>();
        int successfulHandlerCalls = 0;
        catalog.Changed += (_, _) => throw new InvalidOperationException("Handler failed.");
        catalog.Changed += (_, _) => successfulHandlerCalls++;

        Exception? exception = CaptureException(catalog.Refresh);
        using IDiagnosticCatalogSnapshotLease lease = catalog.AcquireSnapshot();

        await Assert.That(exception).IsNull();
        await Assert.That(successfulHandlerCalls).IsEqualTo(1);
        await Assert.That(lease.Version).IsEqualTo(2);
    }

    [Test]
    public async Task Snapshot_factory_disposes_partial_result_when_provider_throws()
    {
        ThrowingRegistrationProvider provider = new();
        using ServiceProvider services = CreateServices(configure: collection =>
            collection.AddSingleton<IDiagnosticRegistrationProvider>(provider));
        Exception? exception = CaptureException(
            () => services.GetRequiredService<IDiagnosticCatalog>());

        await Assert.That(exception).IsTypeOf<InvalidOperationException>();
        await Assert.That(provider.Fix.IsDisposed).IsTrue();
    }

    [Test]
    public async Task Catalog_rejects_registration_with_mismatched_component_code()
    {
        ElementDiagnosticId registrationId = new(
            "ELM-REGISTRATION", "Description", "Message", DiagnosticSeverity.Warning,
            true, false, "");
        ElementDiagnosticId diagnosticId = new(
            "ELM-DIAGNOSTIC", "Description", "Message", DiagnosticSeverity.Warning,
            true, false, "");
        ElementDiagnosticRegistration registration = new(
            registrationId,
            new ElementDiagnostic(diagnosticId, DiagnosticFeedback.Valid),
            new ElementFilter(registrationId, true),
            new ElementDocumentFilter(registrationId, true),
            CreateOverride(registrationId, DiagnosticSeverity.Warning, true),
            []);
        using ServiceProvider services = CreateServices(configure: collection =>
            collection.AddSingleton<IDiagnosticRegistrationProvider>(
                new TestRegistrationProvider(elementDiagnostics: [registration])));

        Exception? exception = CaptureException(
            () => services.GetRequiredService<IDiagnosticCatalog>());

        await Assert.That(exception).IsTypeOf<InvalidOperationException>();
        await Assert.That(exception!.Message).Contains("ELM-REGISTRATION");
        await Assert.That(exception.Message).Contains("ELM-DIAGNOSTIC");
    }

    [Test]
    public async Task Snapshot_factory_disposes_components_when_validation_fails()
    {
        ElementDiagnosticId registrationId = new(
            "ELM-INVALID", "Description", "Message", DiagnosticSeverity.Warning,
            true, false, "");
        ElementDiagnosticId diagnosticId = new(
            "ELM-OTHER", "Description", "Message", DiagnosticSeverity.Warning,
            true, false, "");
        TrackingValueStore<ElementDiagnosticOverridesSettings> store = new(new());
        ElementFix fix = new(registrationId);
        ElementDiagnosticRegistration registration = new(
            registrationId,
            new ElementDiagnostic(diagnosticId, DiagnosticFeedback.Valid),
            new ElementFilter(registrationId, true),
            new ElementDocumentFilter(registrationId, true),
            new ElementDiagnosticIdOverride(registrationId, store),
            [fix]);
        using ServiceProvider services = CreateServices(configure: collection =>
            collection.AddSingleton<IDiagnosticRegistrationProvider>(
                new TestRegistrationProvider(elementDiagnostics: [registration])));

        _ = CaptureException(() => services.GetRequiredService<IDiagnosticCatalog>());

        await Assert.That(store.SubscriptionDisposed).IsTrue();
        await Assert.That(fix.IsDisposed).IsTrue();
    }

    [Test]
    public async Task Built_in_provider_creates_complete_registrations()
    {
        ServiceCollection collection = new();
        collection.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        collection.AddSingleton<IRevitTransactionMemoryCache, TestTransactionMemoryCache>();
        collection.AddSingleton<IValueStore<ElementDiagnosticOverridesSettings>>(
            new ValueStoreStub<ElementDiagnosticOverridesSettings>(new()));
        collection.AddElementDiagnostics();
        collection.AddDiagnosticModule();
        await using ServiceProvider services = collection.BuildServiceProvider();

        IDiagnosticCatalog catalog = services.GetRequiredService<IDiagnosticCatalog>();
        using IDiagnosticCatalogSnapshotLease lease = catalog.AcquireSnapshot();
        DiagnosticCatalogSnapshot snapshot = lease.Snapshot;

        await Assert.That(snapshot.ElementDiagnostics.Count).IsGreaterThan(0);
        await Assert.That(snapshot.ElementDiagnostics.All(registration =>
            registration.Identity.Code == registration.Diagnostic.Identity.Code &&
            registration.Identity.Code == registration.Filter.Identity.Code &&
            registration.Identity.Code == registration.DocumentFilter.Identity.Code)).IsTrue();
        await Assert.That(snapshot.ElementDiagnostics.SelectMany(registration => registration.Fixes).Any()).IsTrue();
    }

}