namespace Revit.Linter.Languages.Tests;

public sealed class LogicalFormulaTests
{
    [Theory]
    [InlineData("1 == 1", true)]
    [InlineData("1 != 2", true)]
    [InlineData("1.0000000001 == 1", true)]
    [InlineData("1.0000000001 != 1", false)]
    [InlineData("1.000000002 == 1", false)]
    [InlineData("1.000000002 != 1", true)]
    [InlineData("'text' == 'text'", true)]
    [InlineData("true != false", true)]
    [InlineData("null == null", true)]
    [InlineData("null != null", false)]
    [InlineData("null == 1", false)]
    [InlineData("null != 1", true)]
    [InlineData("null == false", false)]
    [InlineData("null != false", true)]
    [InlineData("3 > 2", true)]
    [InlineData("3 >= 3", true)]
    [InlineData("2 < 3", true)]
    [InlineData("2 <= 2", true)]
    [InlineData("true & !false", true)]
    [InlineData("false | true", true)]
    public void Logical_expression_returns_expected_result(string formula, bool expected)
    {
        bool result = FormulaCompiler.Evaluate<bool>(formula);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("isnull(null)", true)]
    [InlineData("isdouble(1)", true)]
    [InlineData("isstring('text')", true)]
    [InlineData("isbool(false)", true)]
    [InlineData("isempty('')", true)]
    [InlineData("isempty('text')", false)]
    [InlineData("isempty(null)", false)]
    [InlineData("isnullorempty('')", true)]
    [InlineData("if(true, true, false)", true)]
    [InlineData("isnull(1)", false)]
    [InlineData("isdouble('1')", false)]
    [InlineData("isstring(true)", false)]
    [InlineData("isbool(1)", false)]
    [InlineData("isnullorempty(null)", true)]
    [InlineData("isnullorempty('text')", false)]
    [InlineData("if(false, true, false)", false)]
    public void Logical_function_returns_expected_result(string formula, bool expected)
    {
        bool result = FormulaCompiler.Evaluate<bool>(formula);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("true | false & false", true)]
    [InlineData("(true | false) & false", false)]
    [InlineData("!false & false | true", true)]
    [InlineData("!(false & false | true)", false)]
    [InlineData("1 < 2 == true", true)]
    [InlineData("1 == 1 & 2 > 1", true)]
    [InlineData("false | 2 + 3 * 4 == 14", true)]
    [InlineData("!(1 > 2)", true)]
    [InlineData("1 < 2 != 3 < 2", true)]
    [InlineData("true&true", true)]
    [InlineData("true\t&\ttrue", true)]
    [InlineData("true\r\n & true", true)]
    [InlineData("true & true & false", false)]
    [InlineData("false | false | true", true)]
    public void Logical_operators_respect_precedence_and_brackets(string formula, bool expected)
    {
        bool result = FormulaCompiler.Evaluate<bool>(formula);

        Assert.Equal(expected, result);
    }
}
