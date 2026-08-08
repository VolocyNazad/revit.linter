using Microsoft.Extensions.Logging;
using Revit.Linter.Languages.Languages;
using StringToExpression;
using System.Linq.Expressions;
using Toolkit.Revit.Extensions;

namespace Revit.Linter.UserDiagnostics;

public class ElementFilterFactory(
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
            logger.LogWarning(ex, "User diagnostic formula compilation error.");
            notifier.Notify();
            return ElementFilterUtils.EmptyFilter;
        }
    }
}
