using StringToExpression.GrammerDefinitions;
using System.Linq.Expressions;

namespace Revit.Linter.Languages.Languages;

/// <summary>
/// Creates the standard grammar profiles used by Revit Linter formulas.
/// </summary>
public static class LanguageDefinitions
{
    /// <summary>
    /// Creates the grammar used for formulas that do not require a Revit object context.
    /// </summary>
    /// <returns>The grammar definitions for common value formulas.</returns>
    public static GrammerDefinition[] CreateCommon()
        => CreateValueDefinitions(CreateCommonFunctions());

    /// <summary>
    /// Creates the grammar used for formulas evaluated against a Revit document.
    /// </summary>
    /// <param name="documentExpression">The expression that supplies the target document.</param>
    /// <returns>The grammar definitions for document formulas.</returns>
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

    /// <summary>
    /// Creates the grammar used for formulas evaluated against a Revit element.
    /// </summary>
    /// <param name="elementExpression">The expression that supplies the target element.</param>
    /// <returns>The grammar definitions for element formulas.</returns>
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

    /// <summary>
    /// Creates the grammar used to build native Revit element filters.
    /// </summary>
    /// <returns>The grammar definitions for element-filter formulas.</returns>
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
