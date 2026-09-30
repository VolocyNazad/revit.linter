using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.ElementFixing.Services;

namespace Revit.Linter.ElementFixing.Tests;

public sealed class ElementFixPipelineDefinitionValidatorTests
{
    private static readonly string[] _supportedTypes = ["Delete"];

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
    }

    private static (string Name, ElementFixStepDefinition[] Steps) Validate(
        string name,
        IReadOnlyList<ElementFixStepDefinition> steps) =>
        ElementFixPipelineDefinitionValidator.Validate(name, steps, _supportedTypes);

    private static ElementFixStepDefinition Step(string type) => new() { Type = type };
}
