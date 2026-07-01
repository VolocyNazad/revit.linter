using Microsoft.Extensions.Logging;
using Revit.Linter.Languages.Languages;
using StringToExpression;
using StringToExpression.GrammerDefinitions;
using System.Linq.Expressions;

namespace Revit.Linter.CollisionDiagnostics;

public class ElementFunctionFactory(ILogger<ElementFunctionFactory> logger)
{
    private static readonly ParameterExpression _elementExpression = Expression.Parameter(typeof(Element));

    public Func<Element, object> Create(string formula) => CreateDelegate(formula);

    private static Language Language => field ??= new(AllLanguageDefinitions());
    private Func<Element, object> CreateDelegate(string formula)
    {
        try
        {
            Expression body = Language.Parse(formula);
            return Expression.Lambda<Func<Element, object>>(body, _elementExpression).Compile();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Collision diagnostic formula compilation error.");
            // todo Реализовать уведомление пользователя 'Ошибка компиляции формулы. Исправьте файл конфигурации и перезапустите Revit'
            return elem => string.Empty;
        }
    }
    private static GrammerDefinition[] AllLanguageDefinitions()
    {
        IEnumerable<FunctionCallDefinition> functions = [
           .. ElementFunctionCallDefinitions.Get(_elementExpression),
           .. PropertyFunctionCallDefinitions.Get(_elementExpression),
           .. ArithmeticFunctionCallDefinitions.Get(),
           .. DateTimeFunctionCallDefinitions.Get(),
           .. LogicalFunctionCallDefinitions.Get(),
           .. StringFunctionCallDefinitions.Get(),
        ];

        return [
            .. ArithmeticOperandDefinitions.Get(),
            .. ArithmeticOperatorDefinitions.Get(),
            .. LogicalOperatorDefinitionDefinitions.Get(),
            .. OperandDefinitions.Get(),
            .. ValueStringOperandDefinitions.Get(),
            .. ValueArithmeticOperandDefinitions.Get(),
            .. ValueBooleanOperandDefinitions.Get(),
            .. WhitespaceGrammerDefinitions.Get(),
            .. functions,
            .. BracetGrammerDefinitions.Get(functions),
        ];
    }
}
