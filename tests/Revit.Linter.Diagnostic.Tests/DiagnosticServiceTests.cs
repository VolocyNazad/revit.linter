using Autodesk.Revit.DB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.Diagnostic.Abstractions.Services;
using Revit.Linter.Diagnostic.DI;
using Revit.Linter.Diagnostic.Infrastructure.Exceptions;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Models;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Services;
using Revit.Linter.ElementIgnoring.Abstractions.Services;
using Revit.Linter.ElementDiagnostics.DI;
using Revit.Linter.ValueStore.Abstractions.Services;
using Revit.TransactionMemoryCache.Abstractions.Services;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.Tests;

public sealed class DiagnosticServiceTests : RevitApiTest
{
    private Document? _document;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() => _document = Application.NewProjectDocument(UnitSystem.Metric);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _document?.Close(false);

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

        await Assert.That(catalog.ElementDiagnostics).Count().IsEqualTo(1);
        await Assert.That(catalog.DocumentDiagnostics).Count().IsEqualTo(1);
        await Assert.That(ReferenceEquals(catalog.ElementDiagnostics[0].Fixes.Single(), elementFix)).IsTrue();
        await Assert.That(ReferenceEquals(catalog.DocumentDiagnostics[0].Fixes.Single(), documentFix)).IsTrue();
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
    public async Task Execute_enumerates_requested_element_ids_once()
    {
        Element element = CreateLevel();
        int enumerationCount = 0;
        IEnumerable<ElementId> ElementIds()
        {
            enumerationCount++;
            yield return element.Id;
        }

        ElementDiagnosticRegistration CreateRegistration(string code)
        {
            ElementDiagnosticId id = new(
                code, "Description", "Message", DiagnosticSeverity.Warning,
                true, false, "");
            return new(
                id,
                new ElementDiagnostic(id, DiagnosticFeedback.Valid),
                new ElementFilter(id, true),
                new ElementDocumentFilter(id, true),
                CreateOverride(id, DiagnosticSeverity.Warning, true),
                []);
        }

        using ServiceProvider services = CreateServices(configure: collection =>
            collection.AddSingleton<IDiagnosticRegistrationProvider>(new TestRegistrationProvider(
                elementDiagnostics: [CreateRegistration("ELM-ONE"), CreateRegistration("ELM-TWO")])));

        DiagnosticServiceResult result = services.GetRequiredService<IDiagnosticService>()
            .Execute(_document!, ElementIds());

        await Assert.That(result).IsEqualTo(DiagnosticServiceResult.Success);
        await Assert.That(enumerationCount).IsEqualTo(1);
    }

    [Test]
    public async Task Built_in_provider_creates_complete_registrations()
    {
        ServiceCollection collection = new();
        collection.AddSingleton<IRevitTransactionMemoryCache, TestTransactionMemoryCache>();
        collection.AddSingleton<IValueStore<ElementDiagnosticOverridesSettings>>(
            new ValueStoreStub<ElementDiagnosticOverridesSettings>(new()));
        collection.AddElementDiagnostics();
        collection.AddDiagnosticModule();
        await using ServiceProvider services = collection.BuildServiceProvider();

        IDiagnosticCatalog catalog = services.GetRequiredService<IDiagnosticCatalog>();

        await Assert.That(catalog.ElementDiagnostics.Count).IsGreaterThan(0);
        await Assert.That(catalog.ElementDiagnostics.All(registration =>
            registration.Identity.Code == registration.Diagnostic.Identity.Code &&
            registration.Identity.Code == registration.Filter.Identity.Code &&
            registration.Identity.Code == registration.DocumentFilter.Identity.Code)).IsTrue();
        await Assert.That(catalog.ElementDiagnostics.SelectMany(registration => registration.Fixes).Any()).IsTrue();
    }

    [Test]
    public async Task Document_diagnostic_sends_report_with_feedback_arguments()
    {
        DocumentDiagnosticId id = new(
            "DOC001", "Description", "Value: {value}", DiagnosticSeverity.Warning,
            true, true, "Obsolete");
        DocumentDiagnostic diagnostic = new(id, new(
            DiagnosticVerdict.NotValid,
            new Dictionary<string, object> { ["value"] = 42 }));
        ReportSender sender = new();
        using ServiceProvider services = CreateServices(sender, collection =>
        {
            collection.AddSingleton<IDiagnosticRegistrationProvider>(new TestRegistrationProvider(
                documentDiagnostics:
                [new(id, diagnostic, new DocumentFilter(id, true),
                    CreateOverride(id, DiagnosticSeverity.Error, true), [])]));
        });

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
    public async Task Inactive_document_diagnostic_is_not_executed()
    {
        DocumentDiagnosticId id = new(
            "DOC002", "Description", "Message", DiagnosticSeverity.Warning,
            true, false, "");
        DocumentDiagnostic diagnostic = new(id, new(DiagnosticVerdict.NotValid));
        ReportSender sender = new();
        using ServiceProvider services = CreateServices(sender, collection =>
        {
            collection.AddSingleton<IDiagnosticRegistrationProvider>(new TestRegistrationProvider(
                documentDiagnostics:
                [new(id, diagnostic, new DocumentFilter(id, true),
                    CreateOverride(id, DiagnosticSeverity.Warning, false), [])]));
        });

        DiagnosticServiceResult result = services.GetRequiredService<IDiagnosticService>().Execute(_document!);

        await Assert.That(result).IsEqualTo(DiagnosticServiceResult.Success);
        await Assert.That(diagnostic.ExecutionCount).IsEqualTo(0);
        await Assert.That(sender.Reports).IsEmpty();
    }

    [Test]
    public async Task Duplicate_document_diagnostic_identity_is_rejected_by_catalog()
    {
        DocumentDiagnosticId id = new(
            "DOC003", "Description", "Message", DiagnosticSeverity.Warning,
            true, false, "");
        ReportSender sender = new();
        using ServiceProvider services = CreateServices(sender, collection =>
        {
            collection.AddSingleton<IDiagnosticRegistrationProvider>(new TestRegistrationProvider(
                documentDiagnostics:
                [
                    new(id, new DocumentDiagnostic(id, DiagnosticFeedback.Valid), new DocumentFilter(id, true),
                        CreateOverride(id, DiagnosticSeverity.Warning, true), []),
                    new(id, new DocumentDiagnostic(id, DiagnosticFeedback.Valid), new DocumentFilter(id, true),
                        CreateOverride(id, DiagnosticSeverity.Warning, true), []),
                ]));
        });

        Exception? exception = null;
        try
        {
            _ = services.GetRequiredService<IDiagnosticService>();
        }
        catch (Exception caught)
        {
            exception = caught;
        }

        await Assert.That(exception).IsTypeOf<DuplicateDiagnosticIdException>();
        await Assert.That(sender.Reports).IsEmpty();
    }

    [Test]
    public async Task Element_diagnostic_sends_report_for_requested_element()
    {
        Element element = CreateLevel();
        ElementDiagnosticId id = new(
            "ELM001", "Description", "Value: {value}", DiagnosticSeverity.Warning,
            true, false, "");
        ElementDiagnostic diagnostic = new(id, new(
            DiagnosticVerdict.NotValid,
            new Dictionary<string, object> { ["value"] = "test" },
            "dependency"));
        ReportSender sender = new();
        using ServiceProvider services = CreateServices(sender, collection =>
        {
            collection.AddSingleton<IDiagnosticRegistrationProvider>(new TestRegistrationProvider(
                elementDiagnostics:
                [new(id, diagnostic, new ElementFilter(id, true), new ElementDocumentFilter(id, true),
                    CreateOverride(id, DiagnosticSeverity.Warning, true), [])]));
        });

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
        ElementDiagnosticId id = new(
            "ELM002", "Description", "Message", DiagnosticSeverity.Warning,
            true, false, "");
        ElementDiagnostic diagnostic = new(id, new(DiagnosticVerdict.NotValid));
        ReportSender sender = new();
        using ServiceProvider services = CreateServices(sender, collection =>
        {
            collection.AddSingleton<IIgnoreElementDetector>(new IgnoreElementDetector(true));
            collection.AddSingleton<IDiagnosticRegistrationProvider>(new TestRegistrationProvider(
                elementDiagnostics:
                [new(id, diagnostic, new ElementFilter(id, true), new ElementDocumentFilter(id, true),
                    CreateOverride(id, DiagnosticSeverity.Warning, true), [])]));
        });

        DiagnosticServiceResult result = services.GetRequiredService<IDiagnosticService>()
            .Execute(_document!, [element.Id]);

        await Assert.That(result).IsEqualTo(DiagnosticServiceResult.Success);
        await Assert.That(diagnostic.ExecutionCount).IsEqualTo(0);
        await Assert.That(sender.Reports).IsEmpty();
    }

    private Element CreateLevel()
    {
        using Transaction transaction = new(_document!, "Create level");
        transaction.Start();
        Level level = Level.Create(_document!, 0);
        transaction.Commit();
        return level;
    }

    private static DocumentDiagnosticIdOverride CreateOverride(
        DocumentDiagnosticId id, DiagnosticSeverity severity, bool isActive)
    {
        var settings = new DocumentDiagnosticOverridesSettings();
        settings.Overrides[id.Code] = new DiagnosticOverrideSettings
        {
            Severity = severity,
            IsActive = isActive,
        };
        return new DocumentDiagnosticIdOverride(
            id, new ValueStoreStub<DocumentDiagnosticOverridesSettings>(settings));
    }

    private static ElementDiagnosticIdOverride CreateOverride(
        ElementDiagnosticId id, DiagnosticSeverity severity, bool isActive)
    {
        var settings = new ElementDiagnosticOverridesSettings();
        settings.Overrides[id.Code] = new DiagnosticOverrideSettings
        {
            Severity = severity,
            IsActive = isActive,
        };
        return new ElementDiagnosticIdOverride(
            id, new ValueStoreStub<ElementDiagnosticOverridesSettings>(settings));
    }

    private static object GetArgument(DiagnosticReport report, string name) =>
        report.Message.Args.Single(argument => argument.Item1 == name).Item2;

    private static Exception? CaptureException(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    private static ServiceProvider CreateServices(
        ReportSender? sender = null,
        Action<IServiceCollection>? configure = null)
    {
        sender ??= new ReportSender();
        ServiceCollection services = new();
        services.AddSingleton<IDiagnosticReportSender>(sender);
        services.AddSingleton<IIgnoreElementDetector>(new IgnoreElementDetector(false));
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        configure?.Invoke(services);
        services.AddDiagnosticModule();
        return services.BuildServiceProvider();
    }

    private sealed class ReportSender : IDiagnosticReportSender
    {
        public List<DiagnosticReport> Reports { get; } = [];
        public void Send(DiagnosticReport report) => Reports.Add(report);
    }

    private sealed class DocumentDiagnostic(DocumentDiagnosticId identity, DiagnosticFeedback feedback)
        : IDocumentDiagnostic
    {
        public DocumentDiagnosticId Identity { get; } = identity;
        public int ExecutionCount { get; private set; }
        public DiagnosticFeedback Execute(Document targetDocument)
        {
            ExecutionCount++;
            return feedback;
        }
    }

    private sealed class DocumentFilter(DocumentDiagnosticId identity, bool result)
        : IDocumentDiagnosticFilter
    {
        public DocumentDiagnosticId Identity { get; } = identity;
        public bool IsRelevantFor(Document document) => result;
    }

    private sealed class ElementDiagnostic(ElementDiagnosticId identity, DiagnosticFeedback feedback)
        : IElementDiagnostic
    {
        public ElementDiagnosticId Identity { get; } = identity;
        public int ExecutionCount { get; private set; }
        public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
        {
            ExecutionCount++;
            return feedback;
        }
    }

    private sealed class ElementFilter(ElementDiagnosticId identity, bool result)
        : IElementDiagnosticFilter
    {
        public ElementDiagnosticId Identity { get; } = identity;
        public bool IsRelevantFor(Document document, Element element) => result;
    }

    private sealed class ElementDocumentFilter(ElementDiagnosticId identity, bool result)
        : IElementDiagnosticDocumentFilter
    {
        public ElementDiagnosticId Identity { get; } = identity;
        public bool IsRelevantFor(Document document) => result;
    }

    private sealed class ElementFix(ElementDiagnosticId identity) : IElementFix, IDisposable
    {
        public ElementDiagnosticId Identity { get; } = identity;
        public string Value => "Fix element";
        public bool IsDisposed { get; private set; }
        public bool Execute(Element targetElement) => true;
        public void Dispose() => IsDisposed = true;
    }

    private sealed class DocumentFix(DocumentDiagnosticId identity) : IDocumentFix
    {
        public DocumentDiagnosticId Identity { get; } = identity;
        public string Value => "Fix document";
        public bool Execute(Document targetDocument) => true;
    }

    private sealed class IgnoreElementDetector(bool result) : IIgnoreElementDetector
    {
        public bool IsElementIgnored(string code, Element element) => result;
    }

    private sealed class TestRegistrationProvider(
        IReadOnlyList<ElementDiagnosticRegistration>? elementDiagnostics = null,
        IReadOnlyList<DocumentDiagnosticRegistration>? documentDiagnostics = null)
        : IDiagnosticRegistrationProvider
    {
        public IEnumerable<ElementDiagnosticRegistration> GetElementDiagnostics() => elementDiagnostics ?? [];
        public IEnumerable<DocumentDiagnosticRegistration> GetDocumentDiagnostics() => documentDiagnostics ?? [];
    }

    private sealed class ValueStoreStub<T>(T value) : IValueStore<T> where T : class
    {
        public T CurrentValue { get; } = value;
        public IDisposable OnChange(Action<T> listener) => new EmptyDisposable();
        public void Update(Action<T> change) => change(CurrentValue);

        private sealed class EmptyDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }

    private sealed class TrackingValueStore<T>(T value) : IValueStore<T> where T : class
    {
        public T CurrentValue { get; } = value;
        public bool SubscriptionDisposed { get; private set; }
        public IDisposable OnChange(Action<T> listener) => new DisposableCallback(
            () => SubscriptionDisposed = true);
        public void Update(Action<T> change) => change(CurrentValue);

        private sealed class DisposableCallback(Action dispose) : IDisposable
        {
            public void Dispose() => dispose();
        }
    }

    private sealed class TestTransactionMemoryCache : IRevitTransactionMemoryCache
    {
        private readonly Dictionary<object, object?> _items = [];

        public TItem? GetOrCreate<TItem>(object key, Func<TItem> factory)
        {
            if (_items.TryGetValue(key, out object? value))
                return (TItem?)value;

            TItem item = factory();
            _items[key] = item;
            return item;
        }
    }
}
