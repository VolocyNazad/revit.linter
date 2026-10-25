using Microsoft.Extensions.Logging.Abstractions;
using Revit.Linter.ConfigurationPath;
using Revit.Linter.DiagnosticConfiguration.Models;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.Languages.Factories;
using Revit.Linter.Testing;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class ExampleConfigurationFormulaTests
{
    [Test]
    public async Task All_example_formulas_compile_for_their_declared_field()
    {
        CountingFormulaCompilationNotifier notifier = new();
        DocumentFilterFactory documentFilterFactory = new(NullLogger<DocumentFilterFactory>.Instance, notifier);
        ElementFilterFactory elementFilterFactory = new(NullLogger<ElementFilterFactory>.Instance, notifier);
        ElementFunctionFactory elementFunctionFactory = new(
            NullLogger<ElementFunctionFactory>.Instance, notifier);

        foreach (DiagnosticRule rule in Read(ConfigurationPathUtils.DiagnosticsConfigFileName))
            Compile(rule, documentFilterFactory, elementFilterFactory, elementFunctionFactory);

        // The managed practical-tour configuration is a collision rule as well.
        foreach (DiagnosticRule rule in Read(ConfigurationPathUtils.PracticalTourFileName))
            Compile(rule, documentFilterFactory, elementFilterFactory, elementFunctionFactory);

        await Assert.That(notifier.NotificationCount).IsEqualTo(0);
    }

    private static void Compile(
        DiagnosticRule rule,
        DocumentFilterFactory documentFilterFactory,
        ElementFilterFactory elementFilterFactory,
        ElementFunctionFactory elementFunctionFactory)
    {
        switch (rule.Type)
        {
            case "custom":
                _ = documentFilterFactory.Create(rule.TakeDocument!);
                _ = elementFilterFactory.Create(rule.Take!);
                _ = elementFunctionFactory.Create(rule.Check!, fallback: true);
                break;

            case "collision":
                _ = documentFilterFactory.Create(rule.TakeDocument!);
                _ = elementFilterFactory.Create(rule.Take!);
                _ = elementFilterFactory.Create(rule.AndTake!);
                _ = elementFunctionFactory.Create<object>(rule.GroupBy!, string.Empty);
                break;

            case "parameter":
                _ = documentFilterFactory.Create(rule.Take!);
                break;
        }
    }

    private static IReadOnlyList<DiagnosticRule> Read(string fileName) =>
        ConfigurationPathUtils.GetDiagnosticConfiguration(Path.Combine(
            RepositoryRoot.Find(), "wiki", "examples", "configuration", fileName))?.Rules ?? [];

    private sealed class CountingFormulaCompilationNotifier : IFormulaCompilationNotifier
    {
        public int NotificationCount { get; private set; }
        public void Notify() => NotificationCount++;
    }
}
