using Autodesk.Revit.DB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.Diagnostic.DI;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Models;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Services;
using Revit.Linter.ElementIgnoring.Abstractions.Services;
using Revit.Linter.Testing;
using Toolkit.ValueStore.Abstractions;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

/// <summary>
/// Shared fixture for the diagnostic module tests: a fresh project document per test, a service provider
/// with the diagnostic module, builders for registrations and the test doubles they are made of.
/// </summary>
public abstract class DiagnosticTestBase : RevitApiTest
{
    private protected Document? _document;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() => _document = Application.NewProjectDocument(UnitSystem.Metric);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _document?.Close(false);

    private protected Element CreateLevel(double elevation = 0)
    {
        using Transaction transaction = new(_document!, "Create level");
        transaction.Start();
        Level level = Level.Create(_document!, elevation);
        transaction.Commit();
        return level;
    }

    private protected static ElementDiagnosticId CreateElementId(string code, string message = "Message") =>
        new(code, "Description", message, DiagnosticSeverity.Warning, true, false, "");

    private protected static DocumentDiagnosticId CreateDocumentId(
        string code, string message = "Message", bool isObsolete = false, string obsoleteDescription = "") =>
        new(code, "Description", message, DiagnosticSeverity.Warning, true, isObsolete, obsoleteDescription);

    /// <summary>
    /// Creates an element registration whose components all carry <paramref name="id"/>. Without an explicit
    /// diagnostic the registration reports every element as valid.
    /// </summary>
    private protected static ElementDiagnosticRegistration CreateElementRegistration(
        ElementDiagnosticId id,
        IElementDiagnostic? diagnostic = null,
        bool isActive = true,
        ElementDiagnosticIdOverride? diagnosticOverride = null,
        IReadOnlyList<IElementFix>? fixes = null) =>
        new(
            id,
            diagnostic ?? new ElementDiagnostic(id, DiagnosticFeedback.Valid),
            new ElementFilter(id, true),
            new ElementDocumentFilter(id, true),
            diagnosticOverride ?? CreateOverride(id, DiagnosticSeverity.Warning, isActive),
            fixes ?? [],
            []);

    /// <summary>
    /// Creates a document registration whose components all carry <paramref name="id"/>. Without an explicit
    /// diagnostic the registration reports the document as valid.
    /// </summary>
    private protected static DocumentDiagnosticRegistration CreateDocumentRegistration(
        DocumentDiagnosticId id,
        IDocumentDiagnostic? diagnostic = null,
        bool isActive = true,
        DiagnosticSeverity severity = DiagnosticSeverity.Warning,
        IReadOnlyList<IDocumentFix>? fixes = null) =>
        new(
            id,
            diagnostic ?? new DocumentDiagnostic(id, DiagnosticFeedback.Valid),
            new DocumentFilter(id, true),
            CreateOverride(id, severity, isActive),
            fixes ?? []);

    private protected static DocumentDiagnosticIdOverride CreateOverride(
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

    private protected static ElementDiagnosticIdOverride CreateOverride(
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

    private protected static object GetArgument(DiagnosticReport report, string name) =>
        report.Message.Args.Single(argument => argument.Item1 == name).Item2;

    private protected static Exception? CaptureException(Action action)
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

    /// <summary>
    /// Builds a provider with the diagnostic module. The registrations, when given, are exposed through one
    /// registration provider; <paramref name="configure"/> can add further services before the module.
    /// </summary>
    private protected static ServiceProvider CreateServices(
        ReportSender? sender = null,
        Action<IServiceCollection>? configure = null,
        IReadOnlyList<ElementDiagnosticRegistration>? elementDiagnostics = null,
        IReadOnlyList<DocumentDiagnosticRegistration>? documentDiagnostics = null)
    {
        sender ??= new ReportSender();
        ServiceCollection services = new();
        services.AddSingleton<IDiagnosticReportSender>(sender);
        services.AddSingleton<IIgnoreElementDetector>(new IgnoreElementDetector(false));
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(TestDocumentQueries.Create());
        if (elementDiagnostics is not null || documentDiagnostics is not null)
            services.AddSingleton<IDiagnosticRegistrationProvider>(
                new TestRegistrationProvider(elementDiagnostics, documentDiagnostics));
        configure?.Invoke(services);
        services.AddDiagnosticModule();
        return services.BuildServiceProvider();
    }

    private protected sealed class ReportSender : IDiagnosticReportSender
    {
        public List<DiagnosticReport> Reports { get; } = [];
        public void Send(DiagnosticReport report) => Reports.Add(report);
        public void SendMany(IReadOnlyList<DiagnosticReport> reports) => Reports.AddRange(reports);
    }

    private protected sealed class DocumentDiagnostic(
        DocumentDiagnosticId identity, params DiagnosticFeedback[] feedbacks)
        : IDocumentDiagnostic
    {
        public DocumentDiagnosticId Identity { get; } = identity;
        public int ExecutionCount { get; private set; }
        public IEnumerable<DiagnosticFeedback> Execute(Document targetDocument)
        {
            ExecutionCount++;
            return feedbacks;
        }
    }

    private protected sealed class DocumentFilter(DocumentDiagnosticId identity, bool result)
        : IDocumentDiagnosticFilter
    {
        public DocumentDiagnosticId Identity { get; } = identity;
        public bool IsRelevantFor(Document document) => result;
    }

    private protected sealed class ElementDiagnostic(ElementDiagnosticId identity, DiagnosticFeedback feedback)
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

    private protected sealed class ElementFilter(ElementDiagnosticId identity, bool result)
        : IElementDiagnosticFilter
    {
        public ElementDiagnosticId Identity { get; } = identity;
        public bool IsRelevantFor(Document document, Element element) => result;
    }

    private protected sealed class ElementDocumentFilter(ElementDiagnosticId identity, bool result)
        : IElementDiagnosticDocumentFilter
    {
        public ElementDiagnosticId Identity { get; } = identity;
        public bool IsRelevantFor(Document document) => result;
    }

    private protected sealed class ElementFix(ElementDiagnosticId identity) : IElementFix, IDisposable
    {
        public ElementDiagnosticId Identity { get; } = identity;
        public string Value => "Fix element";
        public bool IsDisposed { get; private set; }
        public bool Execute(Element targetElement) => true;
        public void Dispose() => IsDisposed = true;
    }

    private protected sealed class DocumentFix(DocumentDiagnosticId identity) : IDocumentFix
    {
        public DocumentDiagnosticId Identity { get; } = identity;
        public string Value => "Fix document";
        public bool Execute(Document targetDocument) => true;
    }

    private protected sealed class IgnoreElementDetector(bool result) : IIgnoreElementDetector, IIgnoredElements
    {
        public bool IsElementIgnored(string code, Element element) => result;
        public IIgnoredElements GetIgnoredElements(Document document) => this;
        public bool IsIgnored(string code, Element element) => result;
    }

    private protected sealed class TestRegistrationProvider(
        IReadOnlyList<ElementDiagnosticRegistration>? elementDiagnostics = null,
        IReadOnlyList<DocumentDiagnosticRegistration>? documentDiagnostics = null)
        : IDiagnosticRegistrationProvider
    {
        public IEnumerable<ElementDiagnosticRegistration> GetElementDiagnostics() => elementDiagnostics ?? [];
        public IEnumerable<DocumentDiagnosticRegistration> GetDocumentDiagnostics() => documentDiagnostics ?? [];
    }

    private protected sealed class ValueStoreStub<T>(T value) : IValueStore<T> where T : class
    {
        public T CurrentValue { get; } = value;
        public IDisposable OnChange(Action<T> listener) => new EmptyDisposable();
        public void Update(Action<T> change) => change(CurrentValue);

        private sealed class EmptyDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }
}
