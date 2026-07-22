using Autodesk.Revit.DB;
using Revit.Linter.Languages.Languages;
using StringToExpression;
using StringToExpression.GrammerDefinitions;
using System.Linq.Expressions;

namespace Revit.Linter.Languages.RevitTests;

internal static class PropertyFormulaCompiler
{
    public static Func<TTarget, TResult> Compile<TTarget, TResult>(string formula)
    {
        ParameterExpression targetExpression = Expression.Parameter(typeof(TTarget));
        FunctionCallDefinition[] functions =
        [
            .. PropertyFunctionCallDefinitions.Get(targetExpression),
            .. LogicalFunctionCallDefinitions.Get(),
            .. StringFunctionCallDefinitions.Get(),
        ];

        GrammerDefinition[] definitions =
        [
            .. LogicalOperatorDefinitions.Get(),
            .. OperandDefinitions.Get(),
            .. ValueStringOperandDefinitions.Get(),
            .. ValueBooleanOperandDefinitions.Get(),
            .. WhitespaceGrammarDefinitions.Get(),
            .. functions,
            .. BracketGrammarDefinitions.Get(functions),
        ];

        Expression expression = new Language(definitions).Parse(formula);
        return Expression.Lambda<Func<TTarget, TResult>>(
                Expression.Convert(expression, typeof(TResult)),
                targetExpression)
            .Compile();
    }

    public static Func<Element, TResult> CompileElement<TResult>(string formula)
    {
        ParameterExpression elementExpression = Expression.Parameter(typeof(Element));
        FunctionCallDefinition[] functions =
        [
            .. ElementFunctionCallDefinitions.Get(elementExpression),
            .. PropertyFunctionCallDefinitions.Get(elementExpression),
            .. ArithmeticFunctionCallDefinitions.Get(),
            .. DateTimeFunctionCallDefinitions.Get(),
            .. LogicalFunctionCallDefinitions.Get(),
            .. StringFunctionCallDefinitions.Get(),
        ];

        GrammerDefinition[] definitions = CreateValueDefinitions(functions);
        Expression expression = new Language(definitions).Parse(formula);
        return Expression.Lambda<Func<Element, TResult>>(
                Expression.Convert(expression, typeof(TResult)),
                elementExpression)
            .Compile();
    }

    public static ElementFilter CompileElementFilter(string formula)
    {
        FunctionCallDefinition[] functions = ElementFilterFunctionCallDefinitions.Get();
        GrammerDefinition[] definitions =
        [
            .. ValueStringOperandDefinitions.Get(),
            .. WhitespaceGrammarDefinitions.Get(),
            .. functions,
            .. ElementFilterOperandDefinitions.Get(),
            .. ElementFilterOperatorDefinitions.Get(),
            .. BracketGrammarDefinitions.Get(functions),
        ];

        Expression expression = new Language(definitions).Parse(formula);
        return Expression.Lambda<Func<ElementFilter>>(expression).Compile().Invoke();
    }

    private static GrammerDefinition[] CreateValueDefinitions(FunctionCallDefinition[] functions)
        =>
        [
            .. ArithmeticOperandDefinitions.Get(),
            .. ArithmeticOperatorDefinitions.Get(),
            .. LogicalOperatorDefinitions.Get(),
            .. OperandDefinitions.Get(),
            .. ValueStringOperandDefinitions.Get(),
            .. ValueArithmeticOperandDefinitions.Get(),
            .. ValueBooleanOperandDefinitions.Get(),
            .. WhitespaceGrammarDefinitions.Get(),
            .. functions,
            .. BracketGrammarDefinitions.Get(functions),
        ];
}
