using Microsoft.Extensions.Logging.Abstractions;
using Revit.Linter.ConfigurationPath;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.Testing;
using CollisionDocumentFilterFactory = Revit.Linter.CollisionDiagnostics.DocumentFilterFactory;
using CollisionElementFilterFactory = Revit.Linter.CollisionDiagnostics.ElementFilterFactory;
using CollisionElementFunctionFactory = Revit.Linter.CollisionDiagnostics.ElementFunctionFactory;
using CollisionRule = Revit.Linter.CollisionDiagnostics.Models.DiagnosticRule;
using ParameterDocumentFilterFactory = Revit.Linter.ParameterElementDiagnostics.DocumentFilterFactory;
using ParameterRule = Revit.Linter.ParameterElementDiagnostics.Models.DiagnosticRule;
using UserDocumentFilterFactory = Revit.Linter.UserDiagnostics.DocumentFilterFactory;
using UserElementFilterFactory = Revit.Linter.UserDiagnostics.ElementFilterFactory;
using UserElementFunctionFactory = Revit.Linter.UserDiagnostics.ElementFunctionFactory;
using UserRule = Revit.Linter.UserDiagnostics.Models.DiagnosticRule;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class ExampleConfigurationFormulaTests
{
    [Test]
    public async Task All_example_formulas_compile_for_their_declared_field()
    {
        CountingFormulaCompilationNotifier notifier = new();
        UserDocumentFilterFactory userDocumentFactory = new(
            NullLogger<UserDocumentFilterFactory>.Instance, notifier);
        UserElementFilterFactory userFilterFactory = new(
            NullLogger<UserElementFilterFactory>.Instance, notifier);
        UserElementFunctionFactory userCheckFactory = new(
            NullLogger<UserElementFunctionFactory>.Instance, notifier);
        foreach (UserRule rule in Read<List<UserRule>>("config.yaml")!)
        {
            _ = userDocumentFactory.Create(rule.TakeDocument);
            _ = userFilterFactory.Create(rule.Take);
            _ = userCheckFactory.Create(rule.Check);
        }

        CollisionDocumentFilterFactory collisionDocumentFactory = new(
            NullLogger<CollisionDocumentFilterFactory>.Instance, notifier);
        CollisionElementFilterFactory collisionFilterFactory = new(
            NullLogger<CollisionElementFilterFactory>.Instance, notifier);
        CollisionElementFunctionFactory collisionFunctionFactory = new(
            NullLogger<CollisionElementFunctionFactory>.Instance, notifier);
        foreach (CollisionRule rule in Read<List<CollisionRule>>("collision.config.yaml")!)
        {
            _ = collisionDocumentFactory.Create(rule.TakeDocument);
            _ = collisionFilterFactory.Create(rule.Take);
            _ = collisionFilterFactory.Create(rule.AndTake);
            _ = collisionFunctionFactory.Create(rule.GroupBy);
        }

        ParameterDocumentFilterFactory parameterDocumentFactory = new(
            NullLogger<ParameterDocumentFilterFactory>.Instance, notifier);
        foreach (ParameterRule rule in Read<List<ParameterRule>>("parameter-element.config.yaml")!)
            _ = parameterDocumentFactory.Create(rule.Take);

        await Assert.That(notifier.NotificationCount).IsEqualTo(0);
    }

    private static T? Read<T>(string fileName) where T : class =>
        ConfigurationPathUtils.GetConfigurations<T>(Path.Combine(RepositoryRoot.Find(),
            "wiki", "examples", "configuration", fileName));

    private sealed class CountingFormulaCompilationNotifier : IFormulaCompilationNotifier
    {
        public int NotificationCount { get; private set; }
        public void Notify() => NotificationCount++;
    }
}
