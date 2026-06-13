using Microsoft.Extensions.Logging;
using Revit.Linter.Languages.Languages;
using StringToExpression;
using StringToExpression.GrammerDefinitions;
using System.Linq.Expressions;

namespace Revit.Linter.UserDiagnostics;

public class ElementFunctionFactory(ILogger<ElementFunctionFactory> logger)
{
    private static readonly ParameterExpression _elementExpression = Expression.Parameter(typeof(Element));

    public Func<Element, bool> Create(string formula) => CreateDelegate(formula);

    private static Language Language => field ??= new(AllLanguageDefinitions());
    private Func<Element, bool> CreateDelegate(string formula)
    {
        try
        {
            Expression body = Language.Parse(formula);
            return Expression.Lambda<Func<Element, bool>>(body, _elementExpression).Compile();
        }
        catch (Exception)
        {
            logger.LogWarning("Collision diagnostic formula compilation error.");
            // todo Реализовать уведомление пользователя 'Ошибка компиляции формулы. Исправьте файл конфигурации и перезапустите Revit'
            throw;
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
