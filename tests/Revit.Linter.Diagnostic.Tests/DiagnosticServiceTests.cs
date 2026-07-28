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
using Revit.Linter.DiagnosticReportProvider.Abstractions.Models;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Services;
using Revit.Linter.ElementIgnoring.Abstractions.Services;
using Revit.Linter.ValueStore.Abstractions.Services;
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
            collection.AddSingleton<IDocumentDiagnostic>(diagnostic);
            collection.AddSingleton<IDocumentDiagnosticFilter>(new DocumentFilter(id, true));
            collection.AddSingleton(CreateOverride(id, DiagnosticSeverity.Error, true));
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
            collection.AddSingleton<IDocumentDiagnostic>(diagnostic);
            collection.AddSingleton<IDocumentDiagnosticFilter>(new DocumentFilter(id, true));
            collection.AddSingleton(CreateOverride(id, DiagnosticSeverity.Warning, false));
        });

        DiagnosticServiceResult result = services.GetRequiredService<IDiagnosticService>().Execute(_document!);

        await Assert.That(result).IsEqualTo(DiagnosticServiceResult.Success);
        await Assert.That(diagnostic.ExecutionCount).IsEqualTo(0);
        await Assert.That(sender.Reports).IsEmpty();
    }

    [Test]
    public async Task Duplicate_document_diagnostic_identity_returns_failed()
    {
        DocumentDiagnosticId id = new(
            "DOC003", "Description", "Message", DiagnosticSeverity.Warning,
            true, false, "");
        ReportSender sender = new();
        using ServiceProvider services = CreateServices(sender, collection =>
        {
            collection.AddSingleton<IDocumentDiagnostic>(new DocumentDiagnostic(id, DiagnosticFeedback.Valid));
            collection.AddSingleton<IDocumentDiagnostic>(new DocumentDiagnostic(id, DiagnosticFeedback.Valid));
            collection.AddSingleton<IDocumentDiagnosticFilter>(new DocumentFilter(id, true));
            collection.AddSingleton(CreateOverride(id, DiagnosticSeverity.Warning, true));
        });

        DiagnosticServiceResult result = services.GetRequiredService<IDiagnosticService>().Execute(_document!);

        await Assert.That(result).IsEqualTo(DiagnosticServiceResult.Failed);
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
            collection.AddSingleton<IElementDiagnostic>(diagnostic);
            collection.AddSingleton<IElementDiagnosticFilter>(new ElementFilter(id, true));
            collection.AddSingleton<IElementDiagnosticDocumentFilter>(new ElementDocumentFilter(id, true));
            collection.AddSingleton(CreateOverride(id, DiagnosticSeverity.Warning, true));
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
            collection.AddSingleton<IElementDiagnostic>(diagnostic);
            collection.AddSingleton<IElementDiagnosticFilter>(new ElementFilter(id, true));
            collection.AddSingleton<IElementDiagnosticDocumentFilter>(new ElementDocumentFilter(id, true));
            collection.AddSingleton(CreateOverride(id, DiagnosticSeverity.Warning, true));
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

    private sealed class IgnoreElementDetector(bool result) : IIgnoreElementDetector
    {
        public bool IsElementIgnored(string code, Element element) => result;
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
}
