using System.Linq.Expressions;
using Revit.Linter.Languages.Languages;
using StringToExpression;

namespace Revit.Linter.Languages.Tests;

public sealed class ConditionalFormulaTests
{
    private static readonly Language Language = new(LanguageDefinitions.CreateCommon());

    [Theory]
    [InlineData("if(true, true, false)", true)]
    [InlineData("if(false, true, false)", false)]
    [InlineData("if(true, if(false, false, true), false)", true)]
    [InlineData("if(if(true, false, true), true, false)", false)]
    [InlineData("if(if(false, false, true), true, false)", true)]
    [InlineData("if(false, false, if(true, true, if(false, true, false)))", true)]
    [InlineData("if(true, true, false) & if(false, false, true)", true)]
    [InlineData("isbool(if(true, true, false))", true)]
    public void Nested_conditional_formula_returns_expected_result(string formula, bool expected)
    {
        bool result = FormulaCompiler.Evaluate<bool>(formula);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("if(true, true, false)", typeof(object))]
    [InlineData("if(false, true, if(true, false, true))", typeof(object))]
    [InlineData("if(true, 1, 2)", typeof(object))]
    [InlineData("if(true, 'yes', 'no')", typeof(object))]
    [InlineData("if(true, 'yes', 1)", typeof(object))]
    [InlineData("if(true, null, null)", typeof(object))]
    public void Conditional_formula_has_expected_root_type(string formula, Type expectedType)
    {
        Type rootType = Language.Parse(formula).Type;

        Assert.Equal(expectedType, rootType);
    }

    // The conditional itself evaluates to object; the element check factory converts that root value to a
    // boolean, which is what lets a boxed boolean branch work (for example a parameter value of type bool).
    [Theory]
    [InlineData("if(!contains('Лоджия 1', 'Лоджия'), true, 3 == 3)", true)]
    [InlineData("if(!contains('Кухня', 'Лоджия'), true, 3 == 3)", true)]
    [InlineData("if(!contains('Лоджия 1', 'Лоджия'), true, 5 == 3)", false)]
    public void Check_shaped_conditional_formula_compiles_to_boolean_function(string formula, bool expected)
    {
        Expression body = Language.Parse(formula);
        Func<bool> check = Expression.Lambda<Func<bool>>(
            Expression.Convert(body, typeof(bool))).Compile();

        Assert.Equal(expected, check());
    }
}