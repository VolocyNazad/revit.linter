using Autodesk.Revit.DB;
using Microsoft.Extensions.DependencyInjection;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.ElementIgnoring.Abstractions.Models;
using Revit.Linter.ElementIgnoring.Abstractions.Services;
using Revit.Linter.ElementIgnoring.DI;
using TUnit.Core.Executors;

namespace Revit.Linter.ElementIgnoring.Tests;

public sealed class IgnoreElementManagerTests : RevitApiTest
{
    private static readonly Guid ParameterId = new("666a739a-ae5d-48d1-b146-fc0b2d7f5a4b");
    private static readonly Guid TypeParameterId = new("e1c4d22f-9147-49d5-b7cc-6f13b35e4d53");
    private Document? _document;
    private string? _sharedParameterFile;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() => _document = Application.NewProjectDocument(UnitSystem.Metric);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void Cleanup()
    {
        _document?.Close(false);
        if (_sharedParameterFile is not null && File.Exists(_sharedParameterFile))
            File.Delete(_sharedParameterFile);
    }

    [Test]
    public async Task Dependency_injection_resolves_detector_and_provider_as_same_singleton()
    {
        using ServiceProvider services = CreateServices();

        object detector = services.GetRequiredService<IIgnoreElementDetector>();
        object provider = services.GetRequiredService<IIgnoreElementProvider>();

        await Assert.That(ReferenceEquals(detector, provider)).IsTrue();
        await Assert.That(ReferenceEquals(provider, services.GetRequiredService<IIgnoreElementProvider>())).IsTrue();
    }

    [Test]
    public async Task Element_without_parameter_is_not_ignored()
    {
        Wall wall = CreateWall();
        using ServiceProvider services = CreateServices();

        bool result = services.GetRequiredService<IIgnoreElementDetector>()
            .IsElementIgnored("TEST-001", wall);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task Ignore_fails_when_parameter_is_missing()
    {
        Wall wall = CreateWall();
        using ServiceProvider services = CreateServices();

        IgnoreElementFeedback feedback = services.GetRequiredService<IIgnoreElementProvider>()
            .Ignore("TEST-001", wall);

        await Assert.That(feedback.Result).IsEqualTo(IgnoreElementResult.Failed);
        await Assert.That(feedback.Message).IsEqualTo("Parameter not found.");
    }

    [Test]
    public async Task Ignore_fails_for_non_string_parameter()
    {
        Wall wall = CreateWallWithParameter("INTEGER");
        using ServiceProvider services = CreateServices();

        IgnoreElementFeedback feedback = services.GetRequiredService<IIgnoreElementProvider>()
            .Ignore("TEST-001", wall);
        bool isIgnored = services.GetRequiredService<IIgnoreElementDetector>()
            .IsElementIgnored("TEST-001", wall);

        await Assert.That(feedback.Result).IsEqualTo(IgnoreElementResult.Failed);
        await Assert.That(feedback.Message).IsEqualTo("Parameter storage type not string.");
        await Assert.That(isIgnored).IsFalse();
    }

    [Test]
    public async Task Ignore_writes_code_and_detector_finds_it()
    {
        Wall wall = CreateWallWithParameter("TEXT");
        using ServiceProvider services = CreateServices();
        IIgnoreElementProvider provider = services.GetRequiredService<IIgnoreElementProvider>();
        IIgnoreElementDetector detector = services.GetRequiredService<IIgnoreElementDetector>();

        IgnoreElementFeedback feedback;
        using (Transaction transaction = new(_document!, "Ignore element"))
        {
            transaction.Start();
            feedback = provider.Ignore("TEST-001", wall);
            transaction.Commit();
        }

        await Assert.That(feedback.Result).IsEqualTo(IgnoreElementResult.Success);
        await Assert.That(wall.get_Parameter(ParameterId).AsString()).IsEqualTo("TEST-001");
        await Assert.That(detector.IsElementIgnored("TEST-001", wall)).IsTrue();
    }

    [Test]
    public async Task Ignore_appends_codes_using_separator()
    {
        Wall wall = CreateWallWithParameter("TEXT");
        using ServiceProvider services = CreateServices();
        IIgnoreElementProvider provider = services.GetRequiredService<IIgnoreElementProvider>();

        using (Transaction transaction = new(_document!, "Ignore element"))
        {
            transaction.Start();
            provider.Ignore("FIRST", wall);
            provider.Ignore("SECOND", wall);
            transaction.Commit();
        }

        await Assert.That(wall.get_Parameter(ParameterId).AsString()).IsEqualTo("FIRST;SECOND");
    }

    [Test]
    public async Task Ignore_does_not_duplicate_existing_code()
    {
        Wall wall = CreateWallWithParameter("TEXT");
        using ServiceProvider services = CreateServices();
        IIgnoreElementProvider provider = services.GetRequiredService<IIgnoreElementProvider>();

        using (Transaction transaction = new(_document!, "Ignore element"))
        {
            transaction.Start();
            provider.Ignore("TEST-001", wall);
            provider.Ignore("TEST-001", wall);
            transaction.Commit();
        }

        await Assert.That(wall.get_Parameter(ParameterId).AsString()).IsEqualTo("TEST-001");
    }

    [Test]
    public async Task Detector_matches_complete_code_only()
    {
        Wall wall = CreateWallWithParameter("TEXT");
        using ServiceProvider services = CreateServices();
        IIgnoreElementProvider provider = services.GetRequiredService<IIgnoreElementProvider>();
        IIgnoreElementDetector detector = services.GetRequiredService<IIgnoreElementDetector>();
        using (Transaction transaction = new(_document!, "Ignore element"))
        {
            transaction.Start();
            provider.Ignore("TEST-001", wall);
            transaction.Commit();
        }

        await Assert.That(detector.IsElementIgnored("TEST", wall)).IsFalse();
        await Assert.That(detector.IsElementIgnored("TEST-001", wall)).IsTrue();
    }

    [Test]
    public async Task Element_type_uses_its_own_ignore_parameter()
    {
        Wall wall = CreateWallWithParameter("TEXT");
        ElementType wallType = (ElementType)_document!.GetElement(wall.GetTypeId());
        using ServiceProvider services = CreateServices();
        IIgnoreElementProvider provider = services.GetRequiredService<IIgnoreElementProvider>();
        IIgnoreElementDetector detector = services.GetRequiredService<IIgnoreElementDetector>();

        IgnoreElementFeedback feedback;
        using (Transaction transaction = new(_document, "Ignore element type"))
        {
            transaction.Start();
            feedback = provider.Ignore("TYPE-001", wallType);
            transaction.Commit();
        }

        await Assert.That(wallType.get_Parameter(ParameterId)).IsNull();
        await Assert.That(wallType.get_Parameter(TypeParameterId)).IsNotNull();
        await Assert.That(feedback.Result).IsEqualTo(IgnoreElementResult.Success);
        await Assert.That(detector.IsElementIgnored("TYPE-001", wallType)).IsTrue();
        await Assert.That(detector.IsElementIgnored("TYPE", wallType)).IsFalse();
        await Assert.That(detector.IsElementIgnored("TYPE-001", wall)).IsFalse();
    }

    private Wall CreateWallWithParameter(string dataType)
    {
        (ExternalDefinition definition, ExternalDefinition typeDefinition) = CreateDefinitions(dataType);
        using Transaction transaction = new(_document!, "Create test wall and parameter");
        transaction.Start();
        CategorySet categories = Application.Create.NewCategorySet();
        categories.Insert(Category.GetCategory(_document!, BuiltInCategory.OST_Walls));
        InstanceBinding binding = Application.Create.NewInstanceBinding(categories);
        TypeBinding typeBinding = Application.Create.NewTypeBinding(categories);
#if BEFORE2024
        _document!.ParameterBindings.Insert(definition, binding, BuiltInParameterGroup.PG_DATA);
        _document.ParameterBindings.Insert(typeDefinition, typeBinding, BuiltInParameterGroup.PG_DATA);
#else
        _document!.ParameterBindings.Insert(definition, binding, GroupTypeId.Data);
        _document.ParameterBindings.Insert(typeDefinition, typeBinding, GroupTypeId.Data);
#endif
        Wall wall = CreateWallCore();
        transaction.Commit();
        return wall;
    }

    private Wall CreateWall()
    {
        using Transaction transaction = new(_document!, "Create test wall");
        transaction.Start();
        Wall wall = CreateWallCore();
        transaction.Commit();
        return wall;
    }

    private Wall CreateWallCore()
    {
        Level level = Level.Create(_document!, 0);
        return Wall.Create(
            _document!, Line.CreateBound(XYZ.Zero, new XYZ(10, 0, 0)), level.Id, false);
    }

    private (ExternalDefinition Instance, ExternalDefinition Type) CreateDefinitions(string dataType)
    {
        _sharedParameterFile = Path.Combine(
            Path.GetTempPath(), $"RevitLinter-{Guid.NewGuid():N}.txt");
        File.WriteAllText(_sharedParameterFile, $"""
            # This is a Revit shared parameter file.
            *META	VERSION	MINVERSION
            META	2	1
            *GROUP	ID	NAME
            GROUP	1	Revit Linter Tests
            *PARAM	GUID	NAME	DATATYPE	DATACATEGORY	GROUP	VISIBLE	DESCRIPTION	USERMODIFIABLE	HIDEWHENNOVALUE
            PARAM	{ParameterId:D}	IgnoredDiagnostics	{dataType}		1	1	Ignored diagnostics	1	0
            PARAM	{TypeParameterId:D}	IgnoredTypeDiagnostics	{dataType}		1	1	Ignored type diagnostics	1	0
            """);
        string originalFilename = Application.SharedParametersFilename;
        try
        {
            Application.SharedParametersFilename = _sharedParameterFile;
            DefinitionFile file = Application.OpenSharedParameterFile();
            DefinitionGroup group = file.Groups.get_Item("Revit Linter Tests");
            return (
                (ExternalDefinition)group.Definitions.get_Item("IgnoredDiagnostics"),
                (ExternalDefinition)group.Definitions.get_Item("IgnoredTypeDiagnostics"));
        }
        finally
        {
            Application.SharedParametersFilename = originalFilename;
        }
    }

    private static ServiceProvider CreateServices()
    {
        ServiceCollection services = new();
        services.AddElementIgnoringModule();
        return services.BuildServiceProvider();
    }
}
