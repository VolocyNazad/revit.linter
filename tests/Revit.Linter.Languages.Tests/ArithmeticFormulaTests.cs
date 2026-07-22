namespace Revit.Linter.Languages.Tests;

public sealed class ArithmeticFormulaTests
{
    [Theory]
    [InlineData("1 + 2", 3)]
    [InlineData("7 - 2", 5)]
    [InlineData("3 * 4", 12)]
    [InlineData("8 / 2", 4)]
    [InlineData("7 % 4", 3)]
    [InlineData("2 ^ 3", 8)]
    [InlineData("2 + 3 * 4", 14)]
    [InlineData("(2 + 3) * 4", 20)]
    public void Arithmetic_expression_returns_expected_result(string formula, double expected)
    {
        double result = FormulaCompiler.Evaluate<double>(formula);

        Assert.Equal(expected, result, precision: 10);
    }

    [Theory]
    [InlineData("roundup(1.1)", 2)]
    [InlineData("rounddown(1.9)", 1)]
    [InlineData("round(1.25, 1)", 1.3)]
    [InlineData("sqrt(81)", 9)]
    [InlineData("num('12.5')", 12.5)]
    public void Arithmetic_function_returns_expected_result(string formula, double expected)
    {
        double result = FormulaCompiler.Evaluate<double>(formula);

        Assert.Equal(expected, result, precision: 10);
    }
}
