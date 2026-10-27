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

    /// <summary>Compiles an element check formula into a boolean function.</summary>
    /// <param name="formula">The formula in the element grammar.</param>
    /// <param name="fallback">The value returned for every element when the formula cannot be compiled.</param>
    /// <returns>The compiled check.</returns>
    /// <remarks>
    /// A check formula evaluates to a <see langword="bool"/>; a boxed boolean is accepted as well, because
    /// <c>parameter(...)</c> and <c>if</c> expressions produce one at run time. A formula whose root type is
    /// <see cref="object"/> is therefore converted to <see cref="bool"/> in the expression tree, and a value
    /// of any other run-time type fails the check execution.
    /// </remarks>
    public Func<Element, bool> CreateBoolean(string formula, bool fallback) =>
        FormulaCompilation.CompileOrFallback<Func<Element, bool>>(
            () => Expression.Lambda<Func<Element, bool>>(
                ConvertRootToBoolean(Language.Parse(formula)), ElementParameter).Compile(),
            _ => fallback, formula, logger, notifier);

    /// <summary>Determines whether <paramref name="formula"/> compiles into an element check.</summary>
    /// <param name="formula">The formula in the element grammar.</param>
    /// <returns><see langword="true"/> when the formula compiles; otherwise <see langword="false"/>.</returns>
    /// <remarks>
    /// Uses the same conversion as <see cref="CreateBoolean"/>: a root value typed as <see cref="object"/>
    /// is accepted and converted to <see langword="bool"/> at run time.
    /// </remarks>
    public bool TryValidateBoolean(string formula) =>
        FormulaCompilation.TryCompile(
            () => Expression.Lambda<Func<Element, bool>>(
                ConvertRootToBoolean(Language.Parse(formula)), ElementParameter).Compile(),
            formula, logger);

    private static Expression ConvertRootToBoolean(Expression expression) =>
        expression.Type == typeof(object)
            ? Expression.Convert(expression, typeof(bool))
            : expression;

    /// <summary>Determines whether <paramref name="formula"/> compiles into an element function returning <typeparamref name="TResult"/>.</summary>
    /// <typeparam name="TResult">The type the formula must evaluate to.</typeparam>
    /// <param name="formula">The formula in the element grammar.</param>
    /// <returns><see langword="true"/> when the formula compiles; otherwise <see langword="false"/>.</returns>
    /// <remarks>
    /// The configuration load path uses this check to skip a rule whose formula is invalid and to report
    /// the problem with the rule code and formula text. A failure is only logged here, without the generic
    /// notification that <see cref="Create{TResult}"/> publishes for a fallback at runtime.
    /// </remarks>
    public bool TryValidate<TResult>(string formula) =>
        FormulaCompilation.TryCompile(
            () => Expression.Lambda<Func<Element, TResult>>(Language.Parse(formula), ElementParameter).Compile(),
            formula, logger);
}
