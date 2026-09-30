using Microsoft.Extensions.Logging;
using Revit.Linter.Languages.Languages;
using StringToExpression;
using System.Linq.Expressions;
using Revit.Sugar;

namespace Revit.Linter.UserDiagnostics;

internal sealed class ElementFilterFactory(
    ILogger<ElementFilterFactory> logger,
    IFormulaCompilationNotifier notifier)
{
    public ElementFilter Create(string formula) => CreateDelegate(formula).Invoke();
    private static Language Language => field ??= new(LanguageDefinitions.CreateElementFilter());
    private Func<ElementFilter> CreateDelegate(string formula)
    {
        try
        {
            Expression body = Language.Parse(formula);
            return Expression.Lambda<Func<ElementFilter>>(body).Compile();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to compile user diagnostic formula {Formula}", formula);
            notifier.Notify();
            return ElementFilterUtils.EmptyFilter;
        }
    }
}
