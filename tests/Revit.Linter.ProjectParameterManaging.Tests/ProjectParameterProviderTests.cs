using Autodesk.Revit.DB;
using Microsoft.Extensions.DependencyInjection;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.ProjectParameterManaging.Abstractions.Services;
using Revit.Linter.ProjectParameterManaging.DI;
using TUnit.Core.Executors;
#if BEFORE2024
using Toolkit.Revit.Extensions;
#endif

namespace Revit.Linter.ProjectParameterManaging.Tests;

public sealed class ProjectParameterProviderTests : RevitApiTest
{
    private const string SharedParameterFileName = "required-revit-project-parameters.txt";
    private static readonly Guid ParameterId = new("8d665115-22dd-4a8d-a66c-c123710c9cb2");
    private Document? _document;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() => _document = Application.NewProjectDocument(UnitSystem.Metric);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _document?.Close(false);

    [Test]
    public async Task Dependency_injection_registers_provider_as_singleton()
    {
        ServiceCollection services = new();
        services.AddProjectParameterManagingModule();
        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        IProjectParameterProvider first = serviceProvider.GetRequiredService<IProjectParameterProvider>();
        IProjectParameterProvider second = serviceProvider.GetRequiredService<IProjectParameterProvider>();

        await Assert.That(ReferenceEquals(first, second)).IsTrue();
    }

    [Test]
    public async Task Required_shared_parameter_file_exists_in_output_directory()
    {
        string path = Path.Combine(AppContext.BaseDirectory, SharedParameterFileName);

        await Assert.That(File.Exists(path)).IsTrue();
    }

    [Test]
    public async Task Add_returns_false_for_invalid_document()
    {
        using ServiceProvider services = CreateServices();
        IProjectParameterProvider provider = services.GetRequiredService<IProjectParameterProvider>();

        bool result = Add(provider, null!, [BuiltInCategory.OST_Walls]);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task Add_creates_instance_parameter_for_requested_category()
    {
        using ServiceProvider services = CreateServices();
        IProjectParameterProvider provider = services.GetRequiredService<IProjectParameterProvider>();

        bool result;
        using (Transaction transaction = new(_document!, "Add test project parameter"))
        {
            transaction.Start();
            result = Add(provider, _document!, [BuiltInCategory.OST_Walls]);
            transaction.Commit();
        }

        SharedParameterElement parameter = SharedParameterElement.Lookup(_document!, ParameterId);
        Binding binding = _document!.ParameterBindings.get_Item(parameter.GetDefinition());
        BuiltInCategory[] categories = ((ElementBinding)binding).Categories
            .Cast<Category>().Select(category => category.BuiltInCategory).ToArray();

        await Assert.That(result).IsTrue();
        await Assert.That(binding is InstanceBinding).IsTrue();
        await Assert.That(categories.SequenceEqual([BuiltInCategory.OST_Walls])).IsTrue();
    }

    [Test]
    public async Task Add_returns_true_when_matching_parameter_already_exists()
    {
        using ServiceProvider services = CreateServices();
        IProjectParameterProvider provider = services.GetRequiredService<IProjectParameterProvider>();
        using Transaction transaction = new(_document!, "Add test project parameter");
        transaction.Start();
        _ = Add(provider, _document!, [BuiltInCategory.OST_Walls]);

        bool result = Add(provider, _document!, [BuiltInCategory.OST_Walls]);
        transaction.Commit();

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Add_restores_shared_parameter_filename()
    {
        using ServiceProvider services = CreateServices();
        IProjectParameterProvider provider = services.GetRequiredService<IProjectParameterProvider>();
        string originalFilename = Application.SharedParametersFilename;
        using Transaction transaction = new(_document!, "Add test project parameter");
        transaction.Start();

        _ = Add(provider, _document!, [BuiltInCategory.OST_Walls]);
        transaction.Commit();

        await Assert.That(Application.SharedParametersFilename).IsEqualTo(originalFilename);
    }

    [Test]
    public async Task Add_returns_false_when_parameter_definition_is_not_found()
    {
        using ServiceProvider services = CreateServices();
        IProjectParameterProvider provider = services.GetRequiredService<IProjectParameterProvider>();
        Guid missingParameterId = new("ffffffff-ffff-ffff-ffff-ffffffffffff");
        using Transaction transaction = new(_document!, "Try to add missing parameter");
        transaction.Start();

        bool result = Add(provider, _document!, missingParameterId, [BuiltInCategory.OST_Walls]);
        transaction.RollBack();

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task Add_with_empty_categories_throws_and_restores_shared_parameter_filename()
    {
        using ServiceProvider services = CreateServices();
        IProjectParameterProvider provider = services.GetRequiredService<IProjectParameterProvider>();
        string originalFilename = Application.SharedParametersFilename;
        Exception? exception = null;
        using Transaction transaction = new(_document!, "Try to add parameter without categories");
        transaction.Start();
        try
        {
            _ = Add(provider, _document!, ParameterId, []);
        }
        catch (Exception caught)
        {
            exception = caught;
        }
        transaction.RollBack();

        await Assert.That(exception is InvalidOperationException).IsTrue();
        await Assert.That(Application.SharedParametersFilename).IsEqualTo(originalFilename);
    }

    [Test]
    public async Task Add_updates_existing_parameter_categories()
    {
        using ServiceProvider services = CreateServices();
        IProjectParameterProvider provider = services.GetRequiredService<IProjectParameterProvider>();
        using Transaction transaction = new(_document!, "Update test project parameter categories");
        transaction.Start();
        _ = Add(provider, _document!, [BuiltInCategory.OST_Walls]);

        bool result = Add(provider, _document!, [BuiltInCategory.OST_Doors]);
        transaction.Commit();

        ElementBinding binding = GetBinding();
        BuiltInCategory[] categories = binding.Categories.Cast<Category>()
            .Select(category => category.BuiltInCategory).ToArray();
        await Assert.That(result).IsTrue();
        await Assert.That(categories.SequenceEqual([BuiltInCategory.OST_Doors])).IsTrue();
    }

    [Test]
    public async Task Add_updates_existing_instance_parameter_to_type_parameter()
    {
        using ServiceProvider services = CreateServices();
        IProjectParameterProvider provider = services.GetRequiredService<IProjectParameterProvider>();
        using Transaction transaction = new(_document!, "Update test project parameter binding type");
        transaction.Start();
        _ = Add(provider, _document!, [BuiltInCategory.OST_Walls]);

        bool result = Add(
            provider, _document!, ParameterId, [BuiltInCategory.OST_Walls], isInstance: false);
        transaction.Commit();

        await Assert.That(result).IsTrue();
        await Assert.That(GetBinding() is TypeBinding).IsTrue();
    }

    private ElementBinding GetBinding()
    {
        SharedParameterElement parameter = SharedParameterElement.Lookup(_document!, ParameterId);
        return (ElementBinding)_document!.ParameterBindings.get_Item(parameter.GetDefinition());
    }

    private static ServiceProvider CreateServices()
    {
        ServiceCollection services = new();
        services.AddProjectParameterManagingModule();
        return services.BuildServiceProvider();
    }

    private static bool Add(
        IProjectParameterProvider provider,
        Document document,
        IEnumerable<BuiltInCategory> categories) =>
        Add(provider, document, ParameterId, categories);

    private static bool Add(
        IProjectParameterProvider provider,
        Document document,
        Guid parameterId,
        IEnumerable<BuiltInCategory> categories,
        bool isInstance = true) =>
#if BEFORE2024
        provider.Add(document, parameterId, categories, BuiltInParameterGroup.PG_DATA, isInstance);
#else
        provider.Add(document, parameterId, categories, GroupTypeId.Data, isInstance);
#endif
}
