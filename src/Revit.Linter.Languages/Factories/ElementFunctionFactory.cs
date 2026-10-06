using Microsoft.Extensions.Logging;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.Languages.Languages;
using StringToExpression;
using System.Linq.Expressions;

namespace Revit.Linter.Languages.Factories;

/// <summary>Compiles element formulas into functions of a Revit element.</summary>
/// <param name="logger">Receives a warning for a formula that cannot be compiled.</param>
/// <param name="notifier">Tells the user that a formula could not be compiled.</param>
public sealed class ElementFunctionFactory(
    ILogger<ElementFunctionFactory> logger,
    IFormulaCompilationNotifier notifier)
{
    private static readonly ParameterExpression ElementParameter = Expression.Parameter(typeof(Element));

    private static Language Language => field ??= new(LanguageDefinitions.CreateForElement(ElementParameter));

    /// <summary>Compiles <paramref name="formula"/> into a function of an element.</summary>
    /// <typeparam name="TResult">The type the formula evaluates to.</typeparam>
    /// <param name="formula">The formula in the element grammar.</param>
    /// <param name="fallback">The value returned for every element when the formula cannot be compiled.</param>
    /// <returns>The compiled function.</returns>
    /// <remarks>
    /// A formula that cannot be compiled, including one that evaluates to another type than
    /// <typeparamref name="TResult"/>, is logged and reported through the notifier.
    /// </remarks>
    public Func<Element, TResult> Create<TResult>(string formula, TResult fallback) =>
        FormulaCompilation.CompileOrFallback<Func<Element, TResult>>(
            () => Expression.Lambda<Func<Element, TResult>>(Language.Parse(formula), ElementParameter).Compile(),
            _ => fallback, formula, logger, notifier);
}
