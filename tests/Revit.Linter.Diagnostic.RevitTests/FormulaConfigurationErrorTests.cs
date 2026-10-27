using Autodesk.Revit.DB;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Revit.Linter.CollisionDiagnostics;
using Revit.Linter.CollisionDiagnostics.Abstractions.Infrastructure.Services;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.DocumentQueries.Abstractions.Models;
using Revit.Linter.DocumentQueries.Abstractions.Services;
using Revit.Linter.Languages.Factories;
using Revit.Linter.ParameterElementDiagnostics;
using Revit.Linter.UserDiagnostics;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class FormulaConfigurationErrorTests
{
    [Test]
    public async Task Invalid_check_formula_is_reported_with_rule_code_and_formula()
    {
        const string formula = "if(true,,false,true)==true";
        UserDiagnosticRegistrationProvider provider = CreateUserProvider(
            out RecordingConfigurationErrorState errors,
            out CountingFormulaCompilationNotifier notifier,
            CustomRule(check: formula));

        IReadOnlyList<ElementDiagnosticRegistration> registrations =
            provider.GetElementDiagnostics().ToList();

        await Assert.That(registrations).Count().IsEqualTo(0);
        await Assert.That(errors.CurrentError).Contains("TEST001");
        await Assert.That(errors.CurrentError).Contains(formula);
        await Assert.That(notifier.NotificationCount).IsEqualTo(0);
    }

    [Test]
    public async Task Invalid_take_document_formula_is_reported_with_field_and_formula()
    {
        const string formula = "'text'";
        UserDiagnosticRegistrationProvider provider = CreateUserProvider(
            out RecordingConfigurationErrorState errors,
            out CountingFormulaCompilationNotifier notifier,
            CustomRule(takeDocument: formula));

        IReadOnlyList<ElementDiagnosticRegistration> registrations =
            provider.GetElementDiagnostics().ToList();

        await Assert.That(registrations).Count().IsEqualTo(0);
        await Assert.That(errors.CurrentError).Contains("takeDocument");
        await Assert.That(errors.CurrentError).Contains(formula);
        await Assert.That(notifier.NotificationCount).IsEqualTo(0);
    }

    [Test]
    public async Task Boxed_boolean_take_document_formula_is_not_skipped()
    {
        UserDiagnosticRegistrationProvider provider = CreateUserProvider(
            out RecordingConfigurationErrorState errors,
            out CountingFormulaCompilationNotifier notifier,
            CustomRule(takeDocument: "if(true, true, false)"));

        IReadOnlyList<ElementDiagnosticRegistration> registrations =
            provider.GetElementDiagnostics().ToList();

        await Assert.That(registrations).Count().IsEqualTo(1);
        await Assert.That(errors.CurrentError).IsNull();
        await Assert.That(notifier.NotificationCount).IsEqualTo(0);
    }

    [Test]
    public async Task Invalid_group_by_formula_is_reported_with_field_and_formula()
    {
        const string formula = "1 == 1";
        CountingFormulaCompilationNotifier notifier = new();
        RecordingConfigurationErrorState errors = new();
        CollisionDiagnosticRegistrationProvider provider = new(
            new ElementFilterFactory(NullLogger<ElementFilterFactory>.Instance, notifier),
            new ElementFunctionFactory(NullLogger<ElementFunctionFactory>.Instance, notifier),
            new DocumentFilterFactory(NullLogger<DocumentFilterFactory>.Instance, notifier),
            new ThrowingBoundingBoxService(),
            new ThrowingGeometryService(),
            new ThrowingConnectedElementIdsService(),
            new ThrowingDocumentQueryService(),
            NullLoggerFactory.Instance,
            new EmptyFixPipelineFactory(),
            new EmptyVisualizationPipelineFactory(),
            new SingleRuleConfigurationService(
                new DiagnosticRuleSource(
                    new DiagnosticRule
                    {
                        Type = "collision",
                        Code = "CLSN001",
                        Description = "Invalid grouping",
                        Message = "Invalid grouping",
                        TakeDocument = "true",
                        Take = "instance",
                        AndTake = "instance",
                        GroupBy = formula,
                    },
                    "diagnostics.config.yaml",
                    IsExample: false,
                    IsDebug: false,
                    IsTour: false)),
            errors);

        IReadOnlyList<ElementDiagnosticRegistration> registrations =
            provider.GetElementDiagnostics().ToList();

        await Assert.That(registrations).Count().IsEqualTo(0);
        await Assert.That(errors.CurrentError).Contains("groupBy");
        await Assert.That(errors.CurrentError).Contains(formula);
        await Assert.That(notifier.NotificationCount).IsEqualTo(0);
    }

    [Test]
    public async Task Invalid_parameter_take_formula_is_reported_with_localized_formula_text()
    {
        const string formula = "if(true,,false,true)";
        CountingFormulaCompilationNotifier notifier = new();
        RecordingConfigurationErrorState errors = new();
        ParameterElementDiagnosticRegistrationProvider provider = new(
            new DocumentFilterFactory(NullLogger<DocumentFilterFactory>.Instance, notifier),
            new ThrowingDocumentQueryService(),
            new SingleRuleConfigurationService(
                new DiagnosticRuleSource(
                    new DiagnosticRule
                    {
                        Type = "parameter",
                        Code = "PRM001",
                        Description = "Invalid take",
                        Message = "Invalid take",
                        Take = formula,
                        Parameters =
                        [
                            new DiagnosticParameter
                            {
                                Name = "ADSK_Test",
                                Categories = ["OST_Rooms"],
#if AFTER2024
                                Group = "autodesk.parameter.group:data-1.0.0",
#else
                                Group = "PG_DATA",
#endif
                            },
                        ],
                    },
                    "diagnostics.config.yaml",
                    IsExample: false,
                    IsDebug: false,
                    IsTour: false)),
            errors,
            NullLogger<ParameterElementDiagnosticRegistrationProvider>.Instance);

        IReadOnlyList<DocumentDiagnosticRegistration> registrations =
            provider.GetDocumentDiagnostics().ToList();

        await Assert.That(registrations).Count().IsEqualTo(0);
        await Assert.That(errors.CurrentError).Contains("PRM001");
        await Assert.That(errors.CurrentError).Contains(formula);
        await Assert.That(notifier.NotificationCount).IsEqualTo(0);
    }

    private static DiagnosticRule CustomRule(
        string takeDocument = "true",
        string take = "instance",
        string check = "true") =>
        new()
        {
            Type = "custom",
            Code = "TEST001",
            Description = "Formula validation",
            Message = "Formula validation",
            TakeDocument = takeDocument,
            Take = take,
            Check = check,
        };

    private static UserDiagnosticRegistrationProvider CreateUserProvider(
        out RecordingConfigurationErrorState errors,
        out CountingFormulaCompilationNotifier notifier,
        DiagnosticRule rule)
    {
        errors = new RecordingConfigurationErrorState();
        notifier = new CountingFormulaCompilationNotifier();
        return new UserDiagnosticRegistrationProvider(
            new ElementFilterFactory(NullLogger<ElementFilterFactory>.Instance, notifier),
            new ElementFunctionFactory(NullLogger<ElementFunctionFactory>.Instance, notifier),
            new DocumentFilterFactory(NullLogger<DocumentFilterFactory>.Instance, notifier),
            new EmptyFixPipelineFactory(),
            new EmptyVisualizationPipelineFactory(),
            new SingleRuleConfigurationService(
                new DiagnosticRuleSource(
                    rule,
                    "diagnostics.config.yaml",
                    IsExample: false,
                    IsDebug: false,
                    IsTour: false)),
            errors,
            NullLogger<UserDiagnosticRegistrationProvider>.Instance);
    }

    private sealed class CountingFormulaCompilationNotifier : IFormulaCompilationNotifier
    {
        public int NotificationCount { get; private set; }

        public void Notify() => NotificationCount++;
    }

    private sealed class RecordingConfigurationErrorState : IDiagnosticConfigurationErrorState
    {
        private readonly List<string> _messages = [];

        public string? CurrentError =>
            _messages.Count == 0 ? null : string.Join(Environment.NewLine, _messages);

        public int RuleErrorCount => _messages.Count;

        public event EventHandler? Changed;

        public void BeginRefresh() => _messages.Clear();

        public void SetFileErrors(IReadOnlyList<string> messages, Exception? parseError)
        {
        }

        public void AddRuleErrors(IEnumerable<string> messages)
        {
            _messages.AddRange(messages);
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public void CompleteRefresh()
        {
        }
    }

    private sealed class SingleRuleConfigurationService(params DiagnosticRuleSource[] rules)
        : IDiagnosticConfigurationService
    {
        public string ConfigurationPath => string.Empty;

        public IReadOnlyList<DiagnosticRuleSource> GetRules() => rules;
    }

    private sealed class EmptyFixPipelineFactory : IElementFixPipelineFactory
    {
        public IElementFix Create(
            ElementDiagnosticId identity,
            string name,
            IReadOnlyList<ElementFixStepDefinition> steps) =>
            new EmptyFix(identity);
    }

    private sealed class EmptyFix(ElementDiagnosticId identity) : IElementFix
    {
        public ElementDiagnosticId Identity => identity;

        public string Value => string.Empty;

        public bool Execute(Element targetElement) => true;
    }

    private sealed class EmptyVisualizationPipelineFactory : IElementVisualizationPipelineFactory
    {
        public IElementVisualizationPipeline Create(
            ElementDiagnosticId identity,
            string name,
            IReadOnlyList<ElementVisualizationStepDefinition> steps) =>
            new EmptyVisualizationPipeline(identity);
    }

    private sealed class EmptyVisualizationPipeline(ElementDiagnosticId identity)
        : IElementVisualizationPipeline
    {
        public ElementDiagnosticId Identity => identity;

        public string Value => string.Empty;

        public bool Apply(ElementVisualizationContext context) => true;

        public void Restore()
        {
        }

        public void Dispose()
        {
        }
    }

    private sealed class ThrowingDocumentQueryService : IDocumentQueryService
    {
        public IReadOnlyList<Element> GetElements(Document document, View? view = null) =>
            throw new NotSupportedException();

        public IReadOnlyList<TElement> GetElementsOfClass<TElement>(Document document)
            where TElement : Element =>
            throw new NotSupportedException();

        public IReadOnlyList<Element> GetElementTypes(Document document) =>
            throw new NotSupportedException();

        public GeometryElement? GetGeometry(Element element, View? view = null) =>
            throw new NotSupportedException();

        public TItem GetOrCreate<TItem>(DocumentQueryKey key, Func<TItem> factory) =>
            throw new NotSupportedException();

        public IReadOnlyList<DocumentQueryStatistics> TakeStatistics() =>
            throw new NotSupportedException();
    }

    private sealed class ThrowingBoundingBoxService : IGetElementBoundingBoxService
    {
        public BoundingBoxXYZ Execute(Element element, View? view) =>
            throw new NotSupportedException();
    }

    private sealed class ThrowingGeometryService : IGetElementGeometryService
    {
        public IReadOnlyCollection<Solid> Execute(Element element, View? view) =>
            throw new NotSupportedException();
    }

    private sealed class ThrowingConnectedElementIdsService : IGetConnectedElementIdsService
    {
        public HashSet<long> Execute(Element element) =>
            throw new NotSupportedException();
    }
}
