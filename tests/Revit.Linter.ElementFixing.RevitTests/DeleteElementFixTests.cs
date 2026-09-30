using Autodesk.Revit.DB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.ElementFixing.DI;
using TUnit.Core.Executors;

namespace Revit.Linter.ElementFixing.RevitTests;

public sealed class DeleteElementFixTests : RevitApiTest
{
    private Document? _document;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() => _document = Application.NewProjectDocument(UnitSystem.Metric);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _document?.Close(false);

    [Test]
    public async Task Execute_deletes_target_element()
    {
        Document document = _document!;
        DirectShape target;
        using (Transaction transaction = new(document, "Create test element"))
        {
            transaction.Start();
            ElementId categoryId = Category.GetCategory(document, BuiltInCategory.OST_GenericModel).Id;
            target = DirectShape.CreateElement(document, categoryId);
            transaction.Commit();
        }
        ElementId targetId = target.Id;
        using ServiceProvider provider = CreateProvider();
        IElementFix fix = provider.GetRequiredService<IElementFixPipelineFactory>().Create(
            CreateIdentity(), "Delete", [new ElementFixStepDefinition { Type = "Delete" }]);

        bool result;
        using (Transaction transaction = new(document, "Delete test element"))
        {
            transaction.Start();
            result = ((IElementSetFix)fix).Execute(CreateContext(document, target));
            transaction.Commit();
        }

        await Assert.That(result).IsTrue();
        await Assert.That(document.GetElement(targetId)).IsNull();
    }

    [Test]
    public async Task Execute_deletes_explicit_target_and_dependency_sets()
    {
        Document document = _document!;
        DirectShape target;
        DirectShape dependency;
        using (Transaction transaction = new(document, "Create test elements"))
        {
            transaction.Start();
            ElementId categoryId = Category.GetCategory(document, BuiltInCategory.OST_GenericModel).Id;
            target = DirectShape.CreateElement(document, categoryId);
            dependency = DirectShape.CreateElement(document, categoryId);
            transaction.Commit();
        }
        ElementId targetId = target.Id;
        ElementId dependencyId = dependency.Id;
        using ServiceProvider provider = CreateProvider();
        IElementFix fix = provider.GetRequiredService<IElementFixPipelineFactory>().Create(
            CreateIdentity(), "Delete", [new ElementFixStepDefinition
            {
                Type = "Delete",
                ElementSets = [ElementVisualizationSetKeys.Target, ElementVisualizationSetKeys.Dependencies]
            }]);

        bool result;
        using (Transaction transaction = new(document, "Delete test elements"))
        {
            transaction.Start();
            result = ((IElementSetFix)fix).Execute(CreateContext(document, target, dependency));
            transaction.Commit();
        }

        await Assert.That(result).IsTrue();
        await Assert.That(document.GetElement(targetId)).IsNull();
        await Assert.That(document.GetElement(dependencyId)).IsNull();
    }

    private static ServiceProvider CreateProvider()
    {
        ServiceCollection services = new();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .AddSingleton(typeof(ILogger<>), typeof(Logger<>))
            .AddElementFixing();
        return services.BuildServiceProvider();
    }

    private static ElementDiagnosticId CreateIdentity() => new(
        "TEST", "Test", "Test", DiagnosticSeverity.Message, true, false, string.Empty);

    private static ElementFixContext CreateContext(
        Document document,
        Element target,
        params Element[] dependencies) => new(
        document,
        new Dictionary<string, IReadOnlyCollection<ElementId>>
        {
            [ElementVisualizationSetKeys.Target] = [target.Id],
            [ElementVisualizationSetKeys.Dependencies] = dependencies.Select(element => element.Id).ToArray()
        });
}
