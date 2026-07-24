using Microsoft.Extensions.Logging;
using Revit.Linter.Languages.Languages;
using StringToExpression;
using System.Linq.Expressions;
using Toolkit.Revit.Extensions;

namespace Revit.Linter.CollisionDiagnostics;

public class ElementFilterFactory(ILogger<ElementFilterFactory> logger)
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
            logger.LogWarning(ex, "Collision diagnostic formula compilation error.");
            // todo Реализовать уведомление пользователя 'Ошибка компиляции формулы. Исправьте файл конфигурации и перезапустите Revit'
            return ElementFilterUtils.EmptyFilter;
        }
    }
}
