using Autodesk.Revit.DB;
using Microsoft.Extensions.DependencyInjection;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.ParameterElementDiagnostics;
using Revit.Linter.ParameterElementDiagnostics.Models;
using Revit.Linter.ProjectParameterManaging.Abstractions.Services;
using Revit.Linter.ProjectParameterManaging.DI;
using Revit.Linter.Testing;
using TUnit.Core.Executors;

namespace Revit.Linter.ProjectParameterManaging.RevitTests;

public sealed class ProjectParameterDiagnosticTests : RevitApiTest
{
    private static readonly Guid ParameterId = new("8d665115-22dd-4a8d-a66c-c123710c9cb2");
    private const string ParameterName = "TestProjectParameter";
    private Document? _document;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() => _document = Application.NewProjectDocument(UnitSystem.Metric);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _document?.Close(false);

    [Test]
    public async Task Matching_project_parameter_is_valid()
    {
        AddParameter();
        DocumentDiagnostic diagnostic = CreateDiagnostic(CreateExpectedParameter());

        DiagnosticFeedback feedback = diagnostic.Execute(_document!).Single();

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Numeric_category_and_version_specific_group_identifiers_are_valid()
    {
        AddParameter();
        ParameterElementData expected = CreateExpectedParameter(
            categories: [CategoryIdentifiers.ToNumeric(BuiltInCategory.OST_Walls)],
            group: GetDataGroupIdentifier());
        DocumentDiagnostic diagnostic = CreateDiagnostic(expected);

        DiagnosticFeedback feedback = diagnostic.Execute(_document!).Single();

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Every_mismatched_project_parameter_property_is_reported()
    {
        AddParameter();
        ParameterElementData[] invalidExpectations =
        [
            CreateExpectedParameter(name: "UnexpectedName"),
            CreateExpectedParameter(isInstance: false),
            CreateExpectedParameter(categories: ["OST_Doors"]),
            CreateExpectedParameter(allowVaryBetweenGroups: true),
            CreateExpectedParameter(group: GetDifferentGroupIdentifier())
        ];
        DocumentDiagnostic diagnostic = CreateDiagnostic(invalidExpectations);

        DiagnosticFeedback feedback = diagnostic.Execute(_document!).Single();
        string details = (string)feedback.AdditionalMessageArguments!["details"];

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That(details).Contains("Name");
        await Assert.That(details).Contains("IsInstance");
        await Assert.That(details).Contains("Categories");
        await Assert.That(details).Contains("AllowVaryBetweenGroups");
        await Assert.That(details).Contains("Group");
    }

    [Test]
    public async Task Missing_shared_and_non_shared_parameters_are_reported()
    {
        ParameterElementData missingShared = CreateExpectedParameter(
            guid: "ffffffff-ffff-ffff-ffff-ffffffffffff");
        ParameterElementData missingByName = CreateExpectedParameter(
            guid: string.Empty, name: "MissingParameter");
        DocumentDiagnostic diagnostic = CreateDiagnostic(missingShared, missingByName);

        DiagnosticFeedback feedback = diagnostic.Execute(_document!).Single();
        string details = (string)feedback.AdditionalMessageArguments!["details"];

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That(details).Contains(missingShared.Guid);
        await Assert.That(details).Contains(missingByName.Name);
    }

    [Test]
    public async Task Shared_parameter_without_category_binding_is_reported()
    {
        AddParameter();
        RemoveBinding();
        DocumentDiagnostic diagnostic = CreateDiagnostic(CreateExpectedParameter());

        // The parameter element must outlive its binding; otherwise this would exercise the
        // "parameter not found" path instead of the unbound one.
        await Assert.That(SharedParameterElement.Lookup(_document!, ParameterId)).IsNotNull();

        DiagnosticFeedback feedback = diagnostic.Execute(_document!).Single();
        string details = (string)feedback.AdditionalMessageArguments!["details"];

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That(details).Contains(ParameterName);
        await Assert.That(details).Contains(ParameterId.ToString());
    }

    private void RemoveBinding()
    {
        SharedParameterElement parameter = SharedParameterElement.Lookup(_document!, ParameterId);
        using Transaction transaction = new(_document!, "Remove test project parameter binding");
        transaction.Start();
        bool removed = _document!.ParameterBindings.Remove(parameter.GetDefinition());
        transaction.Commit();
        if (!removed)
            throw new InvalidOperationException("Test project parameter binding could not be removed.");
    }

    private void AddParameter()
    {
        using ServiceProvider services = CreateServices();
        IProjectParameterProvider provider = services.GetRequiredService<IProjectParameterProvider>();
        using Transaction transaction = new(_document!, "Add test project parameter");
        transaction.Start();
#if BEFORE2024
        bool added = provider.Add(
            _document!, ParameterId, [BuiltInCategory.OST_Walls], BuiltInParameterGroup.PG_DATA);
#else
        bool added = provider.Add(
            _document!, ParameterId, [BuiltInCategory.OST_Walls], GroupTypeId.Data);
#endif
        transaction.Commit();
        if (!added)
            throw new InvalidOperationException("Test project parameter could not be added.");
    }

    private static DocumentDiagnostic CreateDiagnostic(params ParameterElementData[] parameters) => new(new TestTransactionMemoryCache())
    {
        Identity = new DocumentDiagnosticId(
            "TEST", "Test", "Test", DiagnosticSeverity.Message, true, false, string.Empty),
        Parameters = parameters
    };

    private static ParameterElementData CreateExpectedParameter(
        string? guid = null,
        string name = ParameterName,
        string? group = null,
        bool isInstance = true,
        List<string>? categories = null,
        bool allowVaryBetweenGroups = false) => new()
    {
        Guid = guid ?? ParameterId.ToString(),
        Name = name,
        Group = group ?? GetDataGroupIdentifier(),
        IsInstance = isInstance,
        Categories = categories ?? ["OST_Walls"],
        AllowVaryBetweenGroups = allowVaryBetweenGroups
    };

    private static string GetDataGroupIdentifier() =>
#if BEFORE2024
        BuiltInParameterGroup.PG_DATA.ToString();
#else
        GroupTypeId.Data.TypeId;
#endif

    private static string GetDifferentGroupIdentifier() =>
#if BEFORE2024
        BuiltInParameterGroup.PG_TEXT.ToString();
#else
        GroupTypeId.Text.TypeId;
#endif

    private static ServiceProvider CreateServices()
    {
        ServiceCollection services = new();
        services.AddProjectParameterManagingModule();
        return services.BuildServiceProvider();
    }
}
