using Microsoft.Extensions.Logging;
using Revit.Linter.Languages.Languages;
using StringToExpression;
using StringToExpression.GrammerDefinitions;
using System.Linq.Expressions;
using Toolkit.Revit.Extensions;

namespace Revit.Linter.UserDiagnostics;

public class ElementFilterFactory(ILogger<ElementFilterFactory> logger)
{
    public ElementFilter Create(string formula) => CreateDelegate(formula).Invoke();
    private static Language Language => field ??= new(AllLanguageDefinitions());
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
    private static GrammerDefinition[] AllLanguageDefinitions()
    {
        IEnumerable<FunctionCallDefinition> functions = [
           .. ElementFilterFunctionCallDefinitions.Get()
        ];

        return [
            .. ValueStringOperandDefinitions.Get(),
            .. WhitespaceGrammerDefinitions.Get(),
            .. functions,
            .. ElementFilterOperandDefinitions.Get(),
            .. ElementFilterOperatorDefinitions.Get(),
            .. BracetGrammerDefinitions.Get(functions),
        ];
    }
}