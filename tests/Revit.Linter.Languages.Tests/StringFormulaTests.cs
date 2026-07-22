namespace Revit.Linter.Languages.Tests;

public sealed class StringFormulaTests
{
    [Theory]
    [InlineData("'Revit' + ' Linter'", "Revit Linter")]
    [InlineData("str(12.5)", "12.5")]
    [InlineData("tolower('ReViT')", "revit")]
    [InlineData("toupper('ReViT')", "REVIT")]
    [InlineData("totitle('revit linter')", "Revit Linter")]
    [InlineData("tosentence('revit linter')", "Revit linter")]
    [InlineData("if(true, 'yes', 'no')", "yes")]
    public void String_expression_returns_expected_result(string formula, string expected)
    {
        string result = FormulaCompiler.Evaluate<string>(formula);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("contains('Revit Linter', 'Linter')", true)]
    [InlineData("startwith('Revit Linter', 'Revit')", true)]
    [InlineData("endwith('Revit Linter', 'Linter')", true)]
    public void String_predicate_returns_expected_result(string formula, bool expected)
    {
        bool result = FormulaCompiler.Evaluate<bool>(formula);

        Assert.Equal(expected, result);
    }
}
