using Microsoft.Extensions.Logging.Abstractions;
using Revit.Linter.ConfigurationPath;
using Revit.Linter.ConfigurationPath.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.Languages.Factories;
using Revit.Linter.Testing;
using CollisionRule = Revit.Linter.CollisionDiagnostics.Models.DiagnosticRule;
using ParameterRule = Revit.Linter.ParameterElementDiagnostics.Models.DiagnosticRule;
using UserRule = Revit.Linter.UserDiagnostics.Models.DiagnosticRule;

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
        foreach (UserRule rule in Read<UserRule>("config.yaml")!.Rules)
        {
            _ = documentFilterFactory.Create(rule.TakeDocument);
            _ = elementFilterFactory.Create(rule.Take);
            _ = elementFunctionFactory.Create(rule.Check, fallback: true);
        }

        foreach (CollisionRule rule in Read<CollisionRule>("collision.config.yaml")!.Rules)
        {
            _ = documentFilterFactory.Create(rule.TakeDocument);
            _ = elementFilterFactory.Create(rule.Take);
            _ = elementFilterFactory.Create(rule.AndTake);
            _ = elementFunctionFactory.Create<object>(rule.GroupBy, string.Empty);
        }

        foreach (ParameterRule rule in Read<ParameterRule>("parameter-element.config.yaml")!.Rules)
            _ = documentFilterFactory.Create(rule.Take);

        await Assert.That(notifier.NotificationCount).IsEqualTo(0);
    }

    private static ConfigurationFile<TRule>? Read<TRule>(string fileName) where TRule : class =>
        ConfigurationPathUtils.GetVersionedConfigurations<TRule>(Path.Combine(RepositoryRoot.Find(),
            "wiki", "examples", "configuration", fileName));

    private sealed class CountingFormulaCompilationNotifier : IFormulaCompilationNotifier
    {
        public int NotificationCount { get; private set; }
        public void Notify() => NotificationCount++;
    }
}
