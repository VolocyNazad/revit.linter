namespace Revit.Linter.Languages.Tests;

public sealed class ArithmeticFormulaTests
{
    [Theory]
    [InlineData("1 + 2", 3)]
    [InlineData("7 - 2", 5)]
    [InlineData("3 * 4", 12)]
    [InlineData("8 / 2", 4)]
    [InlineData("5 % 7", 5)]
    [InlineData("2 ^ 3", 8)]
    [InlineData("2 + 3 * 4", 14)]
    [InlineData("(2 + 3) * 4", 20)]
    [InlineData("0 - 3", -3)]
    [InlineData("7 % 4", 3)]
    [InlineData("2 ^ 0", 1)]
    [InlineData("0", 0)]
    [InlineData("0.0", 0)]
    [InlineData("0.001", 0.001)]
    [InlineData("1000000", 1000000)]
    [InlineData("1+2", 3)]
    [InlineData("1  +  2", 3)]
    [InlineData("1\t+\t2", 3)]
    [InlineData("( 1 + 2 ) * 3", 9)]
    [InlineData("10 - 3 - 2", 5)]
    [InlineData("20 / 5 / 2", 2)]
    [InlineData("20 % 6 % 4", 2)]
    public void Arithmetic_expression_returns_expected_result(string formula, double expected)
    {
        double result = FormulaCompiler.Evaluate<double>(formula);

        Assert.Equal(expected, result, precision: 10);
    }

    [Fact]
    public void Division_by_zero_returns_positive_infinity()
    {
        double result = FormulaCompiler.Evaluate<double>("1 / 0");

        Assert.Equal(double.PositiveInfinity, result);
    }

    [Theory]
    [InlineData("roundup(1.1)", 2)]
    [InlineData("rounddown(1.9)", 1)]
    [InlineData("round(1.25, 1)", 1.3)]
    [InlineData("sqrt(81)", 9)]
    [InlineData("abs(3)", 3)]
    [InlineData("abs(0 - 3)", 3)]
    [InlineData("min(2, 5)", 2)]
    [InlineData("max(2, 5)", 5)]
    [InlineData("num('12.5')", 12.5)]
    [InlineData("sin(0)", 0)]
    [InlineData("cos(0)", 1)]
    [InlineData("tan(0)", 0)]
    [InlineData("pi", Math.PI)]
    public void Arithmetic_function_returns_expected_result(string formula, double expected)
    {
        double result = FormulaCompiler.Evaluate<double>(formula);

        Assert.Equal(expected, result, precision: 10);
    }
}
