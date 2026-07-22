namespace Revit.Linter.Languages.Tests;

public sealed class LogicalFormulaTests
{
    [Theory]
    [InlineData("1 == 1", true)]
    [InlineData("1 != 2", true)]
    [InlineData("1.0000000001 == 1", true)]
    [InlineData("'text' == 'text'", true)]
    [InlineData("true != false", true)]
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
    [InlineData("isnullorempty('')", true)]
    [InlineData("if(true, true, false)", true)]
    public void Logical_function_returns_expected_result(string formula, bool expected)
    {
        bool result = FormulaCompiler.Evaluate<bool>(formula);

        Assert.Equal(expected, result);
    }
}
