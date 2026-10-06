using Microsoft.Extensions.Logging;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.Languages.Languages;
using StringToExpression;
using System.Linq.Expressions;

namespace Revit.Linter.Languages.Factories;

/// <summary>Builds native Revit element filters from element-filter formulas.</summary>
/// <param name="logger">Receives a warning for a formula that cannot be compiled.</param>
/// <param name="notifier">Tells the user that a formula could not be compiled.</param>
public sealed class ElementFilterFactory(
    ILogger<ElementFilterFactory> logger,
    IFormulaCompilationNotifier notifier)
{
    private static Language Language => field ??= new(LanguageDefinitions.CreateElementFilter());

    /// <summary>Builds the element filter described by <paramref name="formula"/>.</summary>
    /// <param name="formula">The formula in the element-filter grammar.</param>
    /// <returns>The filter.</returns>
    /// <remarks>
    /// A formula that cannot be compiled is logged and reported through the notifier, and the returned
    /// filter passes no element.
    /// </remarks>
    public ElementFilter Create(string formula) => FormulaCompilation.CompileOrFallback<Func<ElementFilter>>(
        () => Expression.Lambda<Func<ElementFilter>>(Language.Parse(formula)).Compile(),
        Revit.Sugar.ElementFilterUtils.EmptyFilter, formula, logger, notifier).Invoke();
}
