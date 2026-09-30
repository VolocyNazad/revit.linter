using Autodesk.Revit.DB;
using Microsoft.Extensions.Logging;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.ElementFixing.Abstractions.Services;

namespace Revit.Linter.ElementFixing.Services;

internal sealed class ElementFixPipelineFactory(
    IEnumerable<IElementFixStepExecutor> executors,
    ILogger<ElementFixPipelineFactory> logger)
    : IElementFixPipelineFactory
{
    private readonly IReadOnlyDictionary<string, IElementFixStepExecutor> _executors = executors
        .ToDictionary(executor => executor.Type, StringComparer.OrdinalIgnoreCase);

    public IElementFix Create(
        ElementDiagnosticId identity,
        string name,
        IReadOnlyList<ElementFixStepDefinition> steps)
    {
        (string normalizedName, ElementFixStepDefinition[] normalizedSteps) =
            ElementFixPipelineDefinitionValidator.Validate(
            name, steps, _executors.Keys.ToArray());
        ConfiguredStep[] parsedSteps = normalizedSteps
            .Select(step => new ConfiguredStep(_executors[step.Type], step.ElementSets))
            .ToArray();
        return new ConfiguredElementFixPipeline(identity, normalizedName, parsedSteps, logger);
    }

    private sealed record ConfiguredStep(
        IElementFixStepExecutor Executor,
        IReadOnlyList<string> ElementSetKeys);

    private sealed class ConfiguredElementFixPipeline(
        ElementDiagnosticId identity,
        string value,
        IReadOnlyList<ConfiguredStep> steps,
        ILogger logger)
        : IElementSetFix
    {
        public ElementDiagnosticId Identity { get; } = identity;

        public string Value { get; } = value;

        public bool Execute(Element targetElement) => Execute(new ElementFixContext(
            targetElement.Document,
            new Dictionary<string, IReadOnlyCollection<ElementId>>
            {
                [ElementVisualizationSetKeys.Target] = [targetElement.Id],
                [ElementVisualizationSetKeys.Dependencies] = []
            }));

        public bool Execute(ElementFixContext context)
        {
            ElementId[] targetIds = ResolveElementIds(context, [ElementVisualizationSetKeys.Target]);
            logger.LogInformation(
                "Applying fix pipeline {FixName} for diagnostic {DiagnosticCode}. Targets: {TargetCount}; steps: {StepCount}",
                Value, Identity.Code, targetIds.Length, steps.Count);

            for (int index = 0; index < steps.Count; index++)
            {
                ConfiguredStep step = steps[index];
                IReadOnlyList<string> elementSetKeys = step.ElementSetKeys.Count == 0
                    ? [ElementVisualizationSetKeys.Target]
                    : step.ElementSetKeys;
                ElementId[] elementIds = ResolveElementIds(context, elementSetKeys);
                bool succeeded;
                try
                {
                    succeeded = step.Executor.Execute(context.Document, elementIds);
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        exception,
                        "Fix pipeline {FixName} for diagnostic {DiagnosticCode} failed at step {StepNumber}/{StepCount}: " +
                        "{StepType}; element sets: {ElementSetKeys}",
                        Value, Identity.Code, index + 1, steps.Count, step.Executor.Type,
                        string.Join(", ", elementSetKeys));
                    throw new InvalidOperationException(
                        $"Fix step '{step.Executor.Type}' failed in pipeline '{Value}' for diagnostic '{Identity.Code}'.",
                        exception);
                }

                if (!succeeded)
                {
                    logger.LogWarning(
                        "Fix pipeline {FixName} for diagnostic {DiagnosticCode} failed at step {StepNumber}/{StepCount}: {StepType}",
                        Value, Identity.Code, index + 1, steps.Count, step.Executor.Type);
                    return false;
                }
            }

            logger.LogInformation(
                "Fix pipeline {FixName} for diagnostic {DiagnosticCode} applied to {TargetCount} target elements",
                Value, Identity.Code, targetIds.Length);
            return true;
        }

        private static ElementId[] ResolveElementIds(
            ElementFixContext context,
            IReadOnlyList<string> elementSetKeys) => elementSetKeys
            .Select(key => context.ElementSets.TryGetValue(key, out IReadOnlyCollection<ElementId>? set)
                ? set
                : throw new ArgumentException($"Unknown fix element set '{key}'."))
            .SelectMany(ids => ids)
            .Distinct()
            .ToArray();
    }
}
