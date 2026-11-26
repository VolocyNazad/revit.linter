using Revit.Linter.Presentation.Services;
using Revit.Linter.ReportMessaging;

namespace Revit.Linter.Presentation.Tests;

/// <summary>
/// Verifies that a diagnostic description keeps its sentence and turns every well-formed
/// <c>[text](url)</c> into a hyperlink part without hiding any text.
/// </summary>
public sealed class DiagnosticDescriptionParserTests
{
    [Fact]
    public void Parse_returns_one_text_part_for_a_description_without_a_link()
    {
        const string text = "Checks whether a family has 6 or more types beyond the default type.";

        IReadOnlyList<ReportTextPart> parts = DiagnosticDescriptionParser.Parse(text);

        ReportTextPart part = Assert.Single(parts);
        AssertText(part, text);
    }

    [Fact]
    public void Parse_returns_no_parts_for_an_empty_description()
    {
        Assert.Empty(DiagnosticDescriptionParser.Parse(string.Empty));
        Assert.Empty(DiagnosticDescriptionParser.Parse(null));
    }

    [Fact]
    public void Parse_turns_the_link_into_a_hyperlink_part()
    {
        const string text =
            "Checks a family. [Autodesk Help: Create a Type Catalog](https://example.test/help)";

        IReadOnlyList<ReportTextPart> parts = DiagnosticDescriptionParser.Parse(text);

        Assert.Collection(
            parts,
            part => AssertText(part, "Checks a family. "),
            part => AssertLink(part, "Autodesk Help: Create a Type Catalog", "https://example.test/help"));
    }

    [Fact]
    public void Parse_reads_two_links_and_the_text_between_them()
    {
        const string text =
            "Read [one](https://one.test) and [two](https://two.test) before you continue.";

        IReadOnlyList<ReportTextPart> parts = DiagnosticDescriptionParser.Parse(text);

        Assert.Collection(
            parts,
            part => AssertText(part, "Read "),
            part => AssertLink(part, "one", "https://one.test"),
            part => AssertText(part, " and "),
            part => AssertLink(part, "two", "https://two.test"),
            part => AssertText(part, " before you continue."));
    }

    [Fact]
    public void Parse_keeps_an_unbalanced_bracket_as_text()
    {
        const string text = "A [broken description";

        IReadOnlyList<ReportTextPart> parts = DiagnosticDescriptionParser.Parse(text);

        AssertText(Assert.Single(parts), text);
    }

    [Fact]
    public void Parse_keeps_brackets_that_do_not_start_a_link_as_text()
    {
        const string text = "Types [A] and [B] are checked.";

        IReadOnlyList<ReportTextPart> parts = DiagnosticDescriptionParser.Parse(text);

        AssertText(Assert.Single(parts), text);
    }

    [Fact]
    public void Parse_keeps_a_link_with_a_non_web_address_as_text()
    {
        const string text = "Open [the file](file:///C:/data.txt).";

        IReadOnlyList<ReportTextPart> parts = DiagnosticDescriptionParser.Parse(text);

        AssertText(Assert.Single(parts), text);
    }

    [Fact]
    public void Parse_keeps_a_link_with_empty_text_as_text()
    {
        const string text = "A [](https://example.test) link.";

        IReadOnlyList<ReportTextPart> parts = DiagnosticDescriptionParser.Parse(text);

        AssertText(Assert.Single(parts), text);
    }

    [Fact]
    public void Parse_reads_an_address_with_parentheses()
    {
        const string text = "[Wiki](https://example.test/a_(b))";

        IReadOnlyList<ReportTextPart> parts = DiagnosticDescriptionParser.Parse(text);

        AssertLink(Assert.Single(parts), "Wiki", "https://example.test/a_(b)");
    }

    [Fact]
    public void Parse_keeps_an_address_with_an_unbalanced_parenthesis_as_text()
    {
        const string text = "A [link](https://example.test/a_(b) here.";

        IReadOnlyList<ReportTextPart> parts = DiagnosticDescriptionParser.Parse(text);

        AssertText(Assert.Single(parts), text);
    }

    [Theory]
    [InlineData("http://example.test", true)]
    [InlineData("https://example.test/help", true)]
    [InlineData("file:///C:/data.txt", false)]
    [InlineData("ftp://example.test/file", false)]
    [InlineData("example.test", false)]
    public void IsWebAddress_accepts_only_absolute_web_addresses(string url, bool expected)
        => Assert.Equal(expected, DiagnosticDescriptionParser.IsWebAddress(url));

    private static void AssertText(ReportTextPart part, string text)
    {
        Assert.Equal(ReportInlineType.Text, part.Type);
        Assert.Equal(text, part.Text);
        Assert.Null(part.Data);
    }

    private static void AssertLink(ReportTextPart part, string text, string url)
    {
        Assert.Equal(ReportInlineType.Hyperlink, part.Type);
        Assert.Equal(text, part.Text);
        Assert.Equal(url, part.Data);
    }
}
