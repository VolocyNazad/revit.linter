using Revit.Linter.DocumentDiagnostics.Diagnostics.SizeTables;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class SizeLookupFormulaParserTests
{
    [Test]
    public async Task Parses_literal_table_and_column()
    {
        IReadOnlyList<SizeLookupCall> calls = SizeLookupFormulaParser.Parse(
            "size_lookup(\"Sizes\", \"Length\", 0 mm, Length)", "L");

        await Assert.That(calls.Count).IsEqualTo(1);
        await Assert.That(calls[0].FormulaParameterName).IsEqualTo("L");
        await Assert.That(calls[0].Table.Literal).IsEqualTo("Sizes");
        await Assert.That(calls[0].Column.Literal).IsEqualTo("Length");
    }

    [Test]
    public async Task Parses_parameter_table_and_column()
    {
        IReadOnlyList<SizeLookupCall> calls = SizeLookupFormulaParser.Parse(
            "size_lookup(TableName, Header, 0 mm, Length)", "L");

        await Assert.That(calls.Count).IsEqualTo(1);
        await Assert.That(calls[0].Table.ParameterName).IsEqualTo("TableName");
        await Assert.That(calls[0].Column.ParameterName).IsEqualTo("Header");
    }

    [Test]
    public async Task Parses_bracketed_parameter_names()
    {
        IReadOnlyList<SizeLookupCall> calls = SizeLookupFormulaParser.Parse(
            "size_lookup([Table Name], [Column Name], 0 mm, Length)", "L");

        await Assert.That(calls.Count).IsEqualTo(1);
        await Assert.That(calls[0].Table.ParameterName).IsEqualTo("Table Name");
        await Assert.That(calls[0].Column.ParameterName).IsEqualTo("Column Name");
    }

    [Test]
    public async Task Parses_nested_calls()
    {
        IReadOnlyList<SizeLookupCall> calls = SizeLookupFormulaParser.Parse(
            "size_lookup(\"Sizes\", \"Length\", size_lookup(\"Other\", \"Width\", 0 mm, Length), Length)",
            "L");

        await Assert.That(calls.Count).IsEqualTo(2);
        await Assert.That(calls[0].Table.Literal).IsEqualTo("Sizes");
        await Assert.That(calls[1].Table.Literal).IsEqualTo("Other");
    }

    [Test]
    public async Task Ignores_function_name_inside_a_string()
    {
        IReadOnlyList<SizeLookupCall> calls = SizeLookupFormulaParser.Parse(
            "size_lookup(\"Sizes\", \"Length\", \"size_lookup(Fake, X)\", Length)", "L");

        await Assert.That(calls.Count).IsEqualTo(1);
        await Assert.That(calls[0].Table.Literal).IsEqualTo("Sizes");
    }

    [Test]
    public async Task Leaves_expression_arguments_unresolved()
    {
        IReadOnlyList<SizeLookupCall> calls = SizeLookupFormulaParser.Parse(
            "size_lookup(if(Length > 0 mm, \"A\", \"B\"), \"Length\", 0 mm, Length)", "L");

        await Assert.That(calls.Count).IsEqualTo(1);
        await Assert.That(calls[0].Table.IsResolved).IsFalse();
        await Assert.That(calls[0].Column.Literal).IsEqualTo("Length");
    }

    [Test]
    public async Task Parses_several_calls()
    {
        IReadOnlyList<SizeLookupCall> calls = SizeLookupFormulaParser.Parse(
            "size_lookup(\"A\", \"X\", 0 mm, L) + size_lookup(\"B\", \"Y\", 0 mm, L)", "L");

        await Assert.That(calls.Count).IsEqualTo(2);
        await Assert.That(calls[0].Table.Literal).IsEqualTo("A");
        await Assert.That(calls[1].Table.Literal).IsEqualTo("B");
    }

    [Test]
    public async Task Unescapes_quotes_in_a_literal()
    {
        IReadOnlyList<SizeLookupCall> calls = SizeLookupFormulaParser.Parse(
            "size_lookup(\"3/4\"\" Valve\", \"Length\", 0 mm, L)", "L");

        await Assert.That(calls.Count).IsEqualTo(1);
        await Assert.That(calls[0].Table.Literal).IsEqualTo("3/4\" Valve");
    }

    [Test]
    public async Task Keeps_an_empty_literal()
    {
        IReadOnlyList<SizeLookupCall> calls = SizeLookupFormulaParser.Parse(
            "size_lookup(\"\", \"\", 0 mm, L)", "L");

        await Assert.That(calls.Count).IsEqualTo(1);
        await Assert.That(calls[0].Table.Literal).IsEqualTo(string.Empty);
        await Assert.That(calls[0].Column.Literal).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Ignores_a_longer_identifier()
    {
        IReadOnlyList<SizeLookupCall> calls = SizeLookupFormulaParser.Parse(
            "my_size_lookup(\"Sizes\", \"Length\", 0 mm, L)", "L");

        await Assert.That(calls).IsEmpty();
    }

    [Test]
    public async Task Allows_whitespace_before_the_parenthesis()
    {
        IReadOnlyList<SizeLookupCall> calls = SizeLookupFormulaParser.Parse(
            "size_lookup (\"Sizes\", \"Length\", 0 mm, L)", "L");

        await Assert.That(calls.Count).IsEqualTo(1);
        await Assert.That(calls[0].Table.Literal).IsEqualTo("Sizes");
    }
}
