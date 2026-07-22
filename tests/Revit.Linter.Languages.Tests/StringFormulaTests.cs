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
    [InlineData("'Count: ' + 3", "Count: 3")]
    [InlineData("3 + ' items'", "3 items")]
    [InlineData("'prefix' + null", "prefix")]
    [InlineData("null + 'suffix'", "suffix")]
    [InlineData("str(null)", "")]
    [InlineData("str(true)", "True")]
    [InlineData("''", "")]
    [InlineData("'it\\'s'", "it's")]
    [InlineData("'a\\\\b'", "a\\b")]
    [InlineData("'a\\nb'", "a\nb")]
    [InlineData("'a\\tb'", "a\tb")]
    [InlineData("'a\\rb'", "a\rb")]
    [InlineData("'a\\fb'", "a\fb")]
    [InlineData("'a\\bb'", "a\bb")]
    [InlineData("'a\\qb'", "a\\qb")]
    [InlineData("'a' + 'b' + 'c'", "abc")]
    public void String_expression_returns_expected_result(string formula, string expected)
    {
        string result = FormulaCompiler.Evaluate<string>(formula);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Adding_null_to_null_returns_null()
    {
        object? result = FormulaCompiler.Evaluate<object?>("null + null");

        Assert.Null(result);
    }

    [Theory]
    [InlineData("contains('Revit Linter', 'Linter')", true)]
    [InlineData("startwith('Revit Linter', 'Revit')", true)]
    [InlineData("endwith('Revit Linter', 'Linter')", true)]
    [InlineData("contains('Revit Linter', 'revit')", false)]
    [InlineData("contains('Revit Linter', 'Missing')", false)]
    [InlineData("contains('', 'text')", false)]
    [InlineData("contains('text', '')", true)]
    [InlineData("startwith('Revit Linter', 'Linter')", false)]
    [InlineData("startwith('', 'Revit')", false)]
    [InlineData("endwith('Revit Linter', 'Revit')", false)]
    [InlineData("endwith('', 'Linter')", false)]
    public void String_predicate_returns_expected_result(string formula, bool expected)
    {
        bool result = FormulaCompiler.Evaluate<bool>(formula);

        Assert.Equal(expected, result);
    }
}
