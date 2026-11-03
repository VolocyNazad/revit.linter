using Autodesk.Revit.DB;
using Microsoft.Extensions.Logging;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.ElementFixing.Abstractions.Models;
using Revit.Linter.ElementFixing.Abstractions.Services;
using Revit.Linter.Languages.Factories;
using Revit.Linter.ParameterValues.Models;
using Revit.Linter.ParameterValues.Services;

namespace Revit.Linter.ElementFixing.Services;

internal sealed class SetParameterFixStepExecutor(
    ILogger<SetParameterFixStepExecutor> logger) : IElementFixStepExecutor
{
    public ElementFixStepType Type => ElementFixStepType.SetParameter;

    public IElementFixStep Create(ElementFixStepDefinition step)
    {
        string parameterIdentifier = step.Parameter?.Trim() ?? string.Empty;
        if (parameterIdentifier.Length == 0)
            throw new ArgumentException("The SetParameter step requires the parameter field.");

        string formula = step.Value?.Trim() ?? string.Empty;
        if (formula.Length == 0)
            throw new ArgumentException("The SetParameter step requires the value field.");

        Func<Element, object?> valueFormula;
        try
        {
            valueFormula = ElementFunctionFactory.CreateValue(formula);
        }
        catch (InvalidOperationException exception)
        {
            throw new ArgumentException(exception.Message, exception);
        }

        return new SetParameterFixStep(parameterIdentifier, valueFormula, logger);
    }

    private sealed class SetParameterFixStep(
        string parameterIdentifier,
        Func<Element, object?> valueFormula,
        ILogger logger) : IElementFixStep
    {
        public bool Execute(Document document, IReadOnlyCollection<ElementId> elementIds)
        {
            bool succeeded = true;
            foreach (ElementId elementId in elementIds)
            {
                Element? element = document.GetElement(elementId);
                if (element is not { IsValidObject: true })
                {
                    logger.LogWarning(
                        "Fix step SetParameter skipped element {ElementId} in document {DocumentTitle}: " +
                        "the element no longer exists",
                        elementId, document.Title);
                    succeeded = false;
                    continue;
                }

                Parameter? parameter = ParameterIdentifierResolver.Resolve(element, parameterIdentifier);
                if (parameter is null)
                {
                    logger.LogWarning(
                        "Fix step SetParameter did not find parameter {ParameterIdentifier} on element " +
                        "{ElementId} in document {DocumentTitle}",
                        parameterIdentifier, elementId, document.Title);
                    succeeded = false;
                    continue;
                }

                object? value;
                try
                {
                    value = valueFormula(element);
                }
                catch (Exception exception)
                {
                    logger.LogWarning(
                        exception,
                        "Fix step SetParameter could not evaluate its value for element {ElementId} " +
                        "in document {DocumentTitle}",
                        elementId, document.Title);
                    succeeded = false;
                    continue;
                }

                ParameterWriteResult result = ParameterValueConverter.TryWrite(parameter, value);
                if (!result.Success)
                {
                    logger.LogWarning(
                        result.Failure,
                        "Fix step SetParameter could not write parameter {ParameterIdentifier} of element " +
                        "{ElementId} in document {DocumentTitle}: {Failure}",
                        parameterIdentifier, elementId, document.Title,
                        result.Error is { } error
                            ? ParameterValueErrors.Describe(error)
                            : "unknown failure");
                    succeeded = false;
                    continue;
                }

                if (element is ElementType)
                {
                    logger.LogWarning(
                        "Fix step SetParameter changed parameter {ParameterIdentifier} of element type " +
                        "{ElementId}; every instance of that type changes",
                        parameterIdentifier, elementId);
                }
            }

            return succeeded;
        }
    }
}