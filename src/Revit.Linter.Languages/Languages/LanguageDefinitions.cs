using StringToExpression.GrammerDefinitions;
using System.Linq.Expressions;

namespace Revit.Linter.Languages.Languages;

/// <summary>
/// Creates the standard grammar profiles used by Revit Linter formulas.
/// </summary>
public static class LanguageDefinitions
{
    public static GrammerDefinition[] CreateCommon()
        => CreateValueDefinitions(CreateCommonFunctions());

    public static GrammerDefinition[] CreateForDocument(Expression documentExpression)
    {
        FunctionCallDefinition[] functions =
        [
            .. PropertyFunctionCallDefinitions.Get(documentExpression),
            .. MethodFunctionCallDefinitions.Get(documentExpression),
            .. CreateCommonFunctions(),
        ];

        return CreateValueDefinitions(functions);
    }

    public static GrammerDefinition[] CreateForElement(Expression elementExpression)
    {
        FunctionCallDefinition[] functions =
        [
            .. ElementFunctionCallDefinitions.Get(elementExpression),
            .. PropertyFunctionCallDefinitions.Get(elementExpression),
            .. MethodFunctionCallDefinitions.Get(elementExpression),
            .. CreateCommonFunctions(),
        ];

        return CreateValueDefinitions(functions, ElementDependencyDefinerOperandDefinitions.Get());
    }

    public static GrammerDefinition[] CreateElementFilter()
    {
        FunctionCallDefinition[] functions = ElementFilterFunctionCallDefinitions.Get();

        return
        [
            .. ValueStringOperandDefinitions.Get(),
            .. WhitespaceGrammarDefinitions.Get(),
            .. functions,
            .. ElementFilterOperandDefinitions.Get(),
            .. ElementFilterOperatorDefinitions.Get(),
            .. BracketGrammarDefinitions.Get(functions),
        ];
    }

    private static FunctionCallDefinition[] CreateCommonFunctions()
        =>
        [
            .. ArithmeticFunctionCallDefinitions.Get(),
            .. DateTimeFunctionCallDefinitions.Get(),
            .. LogicalFunctionCallDefinitions.Get(),
            .. StringFunctionCallDefinitions.Get(),
        ];

    private static GrammerDefinition[] CreateValueDefinitions(
        FunctionCallDefinition[] functions,
        params OperandDefinition[] additionalOperands)
        =>
        [
            .. ArithmeticOperandDefinitions.Get(),
            .. ArithmeticOperatorDefinitions.Get(),
            .. LogicalOperatorDefinitions.Get(),
            .. OperandDefinitions.Get(),
            .. ValueStringOperandDefinitions.Get(),
            .. ValueArithmeticOperandDefinitions.Get(),
            .. ValueBooleanOperandDefinitions.Get(),
            .. additionalOperands,
            .. WhitespaceGrammarDefinitions.Get(),
            .. functions,
            .. BracketGrammarDefinitions.Get(functions),
        ];
}
