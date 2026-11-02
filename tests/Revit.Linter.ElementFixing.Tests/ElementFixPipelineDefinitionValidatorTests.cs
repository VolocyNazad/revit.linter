using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.ElementFixing.Services;

namespace Revit.Linter.ElementFixing.Tests;

public sealed class ElementFixPipelineDefinitionValidatorTests
{
    private static readonly string[] _supportedTypes = ["Delete", "SetParameter"];

    [Fact]
    public void Validate_rejects_empty_name()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            Validate(" ", [Step("Delete")]));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Validate_rejects_empty_steps()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Validate("Delete", []));

        Assert.Equal("steps", exception.ParamName);
    }

    [Fact]
    public void Validate_rejects_unknown_step_type()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            Validate("Unsupported", [Step("Replace")]));

        Assert.Contains("Unknown fix step 'Replace'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_rejects_steps_after_delete()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            Validate("Invalid", [Step("Delete"), Step("Delete")]));

        Assert.Equal("steps", exception.ParamName);
    }

    [Fact]
    public void Validate_normalizes_name_and_step_type()
    {
        (string name, ElementFixStepDefinition[] steps) = Validate(
            "  Delete target  ",
            [new() { Type = " delete ", ElementSets = [" Target ", "Target", " "] }]);

        Assert.Equal("Delete target", name);
        Assert.Equal("Delete", steps[0].Type);
        Assert.Equal(["Target"], steps[0].ElementSets);
        Assert.Null(steps[0].Parameter);
        Assert.Null(steps[0].Value);
    }

    [Fact]
    public void Validate_requires_parameter_field_for_set_parameter()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            Validate("Set", [new() { Type = "SetParameter", Value = "'fixed'" }]));

        Assert.Contains("requires the parameter field", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_requires_value_field_for_set_parameter()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            Validate("Set", [new() { Type = "SetParameter", Parameter = "ALL_MODEL_INSTANCE_COMMENTS" }]));

        Assert.Contains("requires the value field", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_normalizes_set_parameter_fields()
    {
        (_, ElementFixStepDefinition[] steps) = Validate(
            "Set",
            [new()
            {
                Type = " setparameter ",
                Parameter = " ALL_MODEL_INSTANCE_COMMENTS ",
                Value = " 'fixed' ",
            }]);

        Assert.Equal("SetParameter", steps[0].Type);
        Assert.Equal("ALL_MODEL_INSTANCE_COMMENTS", steps[0].Parameter);
        Assert.Equal("'fixed'", steps[0].Value);
    }

    [Fact]
    public void Validate_rejects_extra_fields_for_delete()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            Validate("Delete", [new()
            {
                Type = "Delete",
                Parameter = "ALL_MODEL_INSTANCE_COMMENTS",
                Value = "'fixed'",
            }]));

        Assert.Contains("does not support the parameter or value fields", exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_allows_set_parameter_before_delete()
    {
        (_, ElementFixStepDefinition[] steps) = Validate(
            "Set and delete",
            [
                new() { Type = "SetParameter", Parameter = "ALL_MODEL_INSTANCE_COMMENTS", Value = "'fixed'" },
                new() { Type = "Delete" },
            ]);

        Assert.Equal("SetParameter", steps[0].Type);
        Assert.Equal("Delete", steps[1].Type);
    }

    private static (string Name, ElementFixStepDefinition[] Steps) Validate(
        string name,
        IReadOnlyList<ElementFixStepDefinition> steps) =>
        ElementFixPipelineDefinitionValidator.Validate(name, steps, _supportedTypes);

    private static ElementFixStepDefinition Step(string type) => new() { Type = type };
}