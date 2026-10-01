using Autodesk.Revit.DB;
using Revit.Linter.ConfigurationPath;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.ParameterElementDiagnostics.Models;
using Revit.Linter.ParameterElementDiagnostics.Infrastructure.Utils;
using TUnit.Core;

namespace Revit.Linter.ProjectParameterManaging.RevitTests;

public sealed class ExampleConfigurationParsingTests
{
    [Test]
    public async Task Every_supported_parameter_group_value_round_trips_from_configuration_text()
    {
        int count = 0;
#if BEFORE2024
        foreach (BuiltInParameterGroup value in Enum.GetValues(typeof(BuiltInParameterGroup)).Cast<BuiltInParameterGroup>())
        {
            BuiltInParameterGroup parsed = ParameterIdentifierParser.ParseGroup(value.ToString());
            await Assert.That(parsed).IsEqualTo(value);
            count++;
        }
#else
        foreach (System.Reflection.PropertyInfo property in typeof(GroupTypeId)
                     .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                     .Where(property => property.PropertyType == typeof(ForgeTypeId)))
        {
            ForgeTypeId? value = (ForgeTypeId?)property.GetValue(null);
            if (value is not null)
            {
                ForgeTypeId parsed = ParameterIdentifierParser.ParseGroupTypeId(value.TypeId);
                await Assert.That(parsed.TypeId).IsEqualTo(value.TypeId);
                count++;
            }
        }
#endif
        await Assert.That(count).IsGreaterThan(0);
    }

    [Test]
    public async Task Every_supported_category_value_round_trips_from_configuration_text()
    {
        int count = 0;
        foreach (BuiltInCategory value in Enum.GetValues(typeof(BuiltInCategory)).Cast<BuiltInCategory>())
        {
            BuiltInCategory parsed = ParameterIdentifierParser.ParseCategory(value.ToString());
            await Assert.That(parsed).IsEqualTo(value);
            count++;
        }
        await Assert.That(count).IsGreaterThan(0);
    }

    [Test]
    public async Task Every_supported_view_detail_level_round_trips_from_configuration_text()
    {
        int count = 0;
        foreach (ViewDetailLevel value in Enum.GetValues(typeof(ViewDetailLevel)).Cast<ViewDetailLevel>())
        {
            ViewDetailLevel parsed = (ViewDetailLevel)Enum.Parse(typeof(ViewDetailLevel), value.ToString());
            await Assert.That(parsed).IsEqualTo(value);
            count++;
        }
        await Assert.That(count).IsGreaterThan(0);
    }

    [Test]
    public async Task Parameter_identifiers_in_example_configuration_are_convertible()
    {
        List<DiagnosticRule>? rules = ConfigurationPathUtils.GetConfigurations<List<DiagnosticRule>>(
            Path.Combine(FindRepositoryRoot(), "wiki", "examples", "configuration", "parameter-element.config.yaml"));

        await Assert.That(rules).IsNotNull();
        foreach (DiagnosticRule rule in rules!)
        {
            if (!Enum.IsDefined(typeof(DiagnosticSeverity), rule.Severity))
                throw new InvalidOperationException(
                    $"Unknown severity '{rule.Severity}' in rule '{rule.Code}'.");

            foreach (ParameterElementData parameter in rule.Parameters)
            {
                try
                {
                    _ = ParseGroup(parameter.Group);
                    foreach (string category in parameter.Categories)
                        _ = ParameterIdentifierParser.ParseCategory(category);
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException(
                        $"Could not parse parameter identifiers for rule '{rule.Code}', " +
                        $"parameter '{parameter.Name}'. Group: '{parameter.Group}'.", exception);
                }
            }
        }
    }

    private static object ParseGroup(string value) =>
#if BEFORE2024
        ParameterIdentifierParser.ParseGroup(value);
#else
        ParameterIdentifierParser.ParseGroupTypeId(value);
#endif

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Revit.Linter.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
