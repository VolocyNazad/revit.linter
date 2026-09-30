using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.ElementFixing.Services;

internal static class ElementFixPipelineDefinitionValidator
{
    public static (string Name, ElementFixStepDefinition[] Steps) Validate(
        string name,
        IReadOnlyList<ElementFixStepDefinition> steps,
        IReadOnlyCollection<string> supportedTypes)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Fix pipeline name cannot be empty.", nameof(name));
        if (steps.Count == 0)
            throw new ArgumentException("Fix pipeline must contain at least one step.", nameof(steps));

        ElementFixStepDefinition[] normalizedSteps = steps
            .Select(step => Normalize(step, supportedTypes))
            .ToArray();
        if (normalizedSteps.Take(normalizedSteps.Length - 1)
            .Any(step => string.Equals(step.Type, "Delete", StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Delete must be the final fix pipeline step.", nameof(steps));

        return (name.Trim(), normalizedSteps);
    }

    private static ElementFixStepDefinition Normalize(
        ElementFixStepDefinition step,
        IReadOnlyCollection<string> supportedTypes)
    {
        if (string.IsNullOrWhiteSpace(step.Type))
            throw new ArgumentException("Fix step type cannot be empty.");

        string type = step.Type.Trim();
        string? supportedType = supportedTypes.FirstOrDefault(candidate =>
            string.Equals(candidate, type, StringComparison.OrdinalIgnoreCase));
        if (supportedType is null)
            throw new ArgumentException(
                $"Unknown fix step '{step.Type}'. Supported values: {string.Join(", ", supportedTypes)}.");

        string[] elementSets = step.ElementSets
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        return new ElementFixStepDefinition { Type = supportedType, ElementSets = elementSets };
    }
}
