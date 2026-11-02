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

public sealed class SetParameterFixTests : RevitApiTest
{
    private Document? _document;
    private Wall? _wall;
    private Level? _level;

    private Document CurrentDocument => _document!;
    private Wall TargetWall => _wall!;
    private Level TargetLevel => _level!;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateModel()
    {
        _document = Application.NewProjectDocument(UnitSystem.Metric);

        using Transaction transaction = new(_document, "Seed set parameter tests");
        transaction.Start();
        _level = Level.Create(_document, 0);
        _wall = Wall.Create(
            _document,
            Line.CreateBound(XYZ.Zero, new XYZ(10, 0, 0)),
            _level.Id,
            false);
        _wall.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).Set("before");
        transaction.Commit();
    }

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseModel() => _document?.Close(false);

    [Test]
    public async Task Execute_sets_string_parameter()
    {
        using ServiceProvider provider = CreateProvider();
        IElementFix fix = CreateFix(provider, [
            new() { Type = "SetParameter", Parameter = "ALL_MODEL_INSTANCE_COMMENTS", Value = "'fixed'" },
        ]);

        bool result = Execute(fix, TargetWall);

        await Assert.That(result).IsTrue();
        await Assert.That(
                TargetWall.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).AsString())
            .IsEqualTo("fixed");
    }

    [Test]
    public async Task Execute_uses_parameter_value_formula()
    {
        using (Transaction transaction = new(CurrentDocument, "Seed mark"))
        {
            transaction.Start();
            TargetWall.get_Parameter(BuiltInParameter.ALL_MODEL_MARK).Set("mark-value");
            transaction.Commit();
        }

        using ServiceProvider provider = CreateProvider();
        IElementFix fix = CreateFix(provider, [
            new()
            {
                Type = "SetParameter",
                Parameter = "ALL_MODEL_INSTANCE_COMMENTS",
                Value = "parameter(me, 'ALL_MODEL_MARK')",
            },
        ]);

        bool result = Execute(fix, TargetWall);

        await Assert.That(result).IsTrue();
        await Assert.That(
                TargetWall.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).AsString())
            .IsEqualTo("mark-value");
    }

    [Test]
    public async Task Execute_writes_element_id_from_property()
    {
        using ServiceProvider provider = CreateProvider();
        IElementFix fix = CreateFix(provider, [
            new()
            {
                Type = "SetParameter",
                Parameter = "WALL_BASE_CONSTRAINT",
                Value = "property('LevelId')",
            },
        ]);

        bool result = Execute(fix, TargetWall);

        await Assert.That(result).IsTrue();
        await Assert.That(
                TargetWall.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT).AsElementId())
            .IsEqualTo(TargetLevel.Id);
    }

    [Test]
    public async Task Execute_writes_yes_no_parameter_from_boolean()
    {
        using ServiceProvider provider = CreateProvider();
        IElementFix fix = CreateFix(provider, [
            new() { Type = "SetParameter", Parameter = "LEVEL_IS_BUILDING_STORY", Value = "true" },
        ]);

        bool result = Execute(fix, TargetLevel);

        await Assert.That(result).IsTrue();
        await Assert.That(
                TargetLevel.get_Parameter(BuiltInParameter.LEVEL_IS_BUILDING_STORY).AsInteger())
            .IsEqualTo(1);
    }

    [Test]
    public async Task Execute_writes_type_parameter()
    {
        ElementType wallType = (ElementType)CurrentDocument.GetElement(TargetWall.GetTypeId());
        using ServiceProvider provider = CreateProvider();
        IElementFix fix = CreateFix(provider, [
            new()
            {
                Type = "SetParameter",
                Parameter = "ALL_MODEL_TYPE_COMMENTS",
                Value = "'type fixed'",
            },
        ]);

        bool result = Execute(fix, wallType);

        await Assert.That(result).IsTrue();
        await Assert.That(
                wallType.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_COMMENTS).AsString())
            .IsEqualTo("type fixed");
    }

    [Test]
    public async Task Execute_returns_false_when_parameter_is_missing()
    {
        using ServiceProvider provider = CreateProvider();
        IElementFix fix = CreateFix(provider, [
            new() { Type = "SetParameter", Parameter = "Missing parameter", Value = "'fixed'" },
        ]);

        bool result = Execute(fix, TargetWall);

        await Assert.That(result).IsFalse();
        await Assert.That(
                TargetWall.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).AsString())
            .IsEqualTo("before");
    }

    [Test]
    public async Task Execute_returns_false_for_read_only_parameter()
    {
        using ServiceProvider provider = CreateProvider();
        IElementFix fix = CreateFix(provider, [
            new() { Type = "SetParameter", Parameter = "HOST_AREA_COMPUTED", Value = "0.5" },
        ]);

        bool result = Execute(fix, TargetWall);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task Execute_runs_set_parameter_before_delete()
    {
        using ServiceProvider provider = CreateProvider();
        IElementFix fix = CreateFix(provider, [
            new() { Type = "SetParameter", Parameter = "ALL_MODEL_INSTANCE_COMMENTS", Value = "'fixed'" },
            new() { Type = "Delete" },
        ]);
        ElementId wallId = TargetWall.Id;

        bool result = Execute(fix, TargetWall);

        await Assert.That(result).IsTrue();
        await Assert.That(CurrentDocument.GetElement(wallId)).IsNull();
    }

    [Test]
    public async Task Create_rejects_value_formula_that_cannot_be_compiled()
    {
        using ServiceProvider provider = CreateProvider();

        ArgumentException exception = Assert.Throws<ArgumentException>(() => CreateFix(provider, [
            new() { Type = "SetParameter", Parameter = "ALL_MODEL_INSTANCE_COMMENTS", Value = "'unterminated" },
        ]));

        await Assert.That(exception.Message.Contains("cannot be compiled", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Create_rejects_delete_with_parameter_fields()
    {
        using ServiceProvider provider = CreateProvider();

        ArgumentException exception = Assert.Throws<ArgumentException>(() => CreateFix(provider, [
            new()
            {
                Type = "Delete",
                Parameter = "ALL_MODEL_INSTANCE_COMMENTS",
                Value = "'fixed'",
            },
        ]));

        await Assert.That(
                exception.Message.Contains("does not support the parameter or value fields", StringComparison.Ordinal))
            .IsTrue();
    }

    private static ServiceProvider CreateProvider()
    {
        ServiceCollection services = new();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .AddSingleton(typeof(ILogger<>), typeof(Logger<>))
            .AddSingleton<IFormulaCompilationNotifier, NullFormulaCompilationNotifier>()
            .AddElementFixing();
        return services.BuildServiceProvider();
    }

    private static IElementFix CreateFix(ServiceProvider provider, ElementFixStepDefinition[] steps) =>
        provider.GetRequiredService<IElementFixPipelineFactory>().Create(CreateIdentity(), "Set value", steps);

    private bool Execute(IElementFix fix, Element target)
    {
        using Transaction transaction = new(CurrentDocument, "Apply set parameter fix");
        transaction.Start();
        bool result = ((IElementSetFix)fix).Execute(new ElementFixContext(
            CurrentDocument,
            new Dictionary<string, IReadOnlyCollection<ElementId>>
            {
                [ElementVisualizationSetKeys.Target] = [target.Id],
                [ElementVisualizationSetKeys.Dependencies] = [],
            }));
        transaction.Commit();
        return result;
    }

    private static ElementDiagnosticId CreateIdentity() => new(
        "TEST", "Test", "Test", DiagnosticSeverity.Message, true, false, string.Empty);

    private sealed class NullFormulaCompilationNotifier : IFormulaCompilationNotifier
    {
        public void Notify()
        {
        }
    }
}