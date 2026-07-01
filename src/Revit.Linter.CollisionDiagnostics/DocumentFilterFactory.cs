using Microsoft.Extensions.Logging;
using Revit.Linter.Languages.Languages;
using StringToExpression;
using StringToExpression.GrammerDefinitions;
using System.Linq.Expressions;

namespace Revit.Linter.CollisionDiagnostics;

public class DocumentFilterFactory(ILogger<DocumentFilterFactory> logger)
{
    private static readonly ParameterExpression _documentExpression = Expression.Parameter(typeof(Document));

    public Func<Document, bool> Create(string formula) => CreateDelegate(formula);

    private static Language Language => field ??= new(AllLanguageDefinitions());
    private Func<Document, bool> CreateDelegate(string formula)
    {
        try
        {
            Expression body = Language.Parse(formula);
            return Expression.Lambda<Func<Document, bool>>(body, _documentExpression).Compile();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Collision diagnostic formula compilation error.");
            // todo Реализовать уведомление пользователя 'Ошибка компиляции формулы. Исправьте файл конфигурации и перезапустите Revit'
            return doc => false;
        }
    }
    private static GrammerDefinition[] AllLanguageDefinitions()
    {
        IEnumerable<FunctionCallDefinition> functions = [
           .. PropertyFunctionCallDefinitions.Get(_documentExpression),
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