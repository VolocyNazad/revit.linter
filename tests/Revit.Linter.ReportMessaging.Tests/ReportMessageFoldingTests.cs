namespace Revit.Linter.ReportMessaging.Tests;

public sealed class ReportMessageFoldingTests
{
    private const string OpenQuote = "\u201C";
    private const string CloseQuote = "\u201D";
    private const string Marker = "\u2022 ";

    [Fact]
    public void Short_link_list_stays_inline()
    {
        var links = Enumerable.Range(1, 2).Select(static number => new FakeLink(number.ToString())).ToArray();

        var parts = ReportMessageParser.ParseTemplateForDisplay(
            "Elements {ids}",
            new Dictionary<string, object> { ["ids"] = links },
            FakeLinks,
            new ReportDisplayBudget { MaxInlineListItems = 2 });

        Assert.Equal(
        [
            new ReportTextPart(ReportInlineType.Text, "Elements "),
            new ReportTextPart(ReportInlineType.Hyperlink, "1", links[0]),
            new ReportTextPart(ReportInlineType.Text, ", "),
            new ReportTextPart(ReportInlineType.Hyperlink, "2", links[1]),
        ], parts);
    }

    [Fact]
    public void Long_link_list_is_folded_with_the_complete_list_on_the_expander()
    {
        var links = Enumerable.Range(1, 5).Select(static number => new FakeLink(number.ToString())).ToArray();

        var parts = ReportMessageParser.ParseTemplateForDisplay(
            "Elements {ids}",
            new Dictionary<string, object> { ["ids"] = links },
            FakeLinks,
            new ReportDisplayBudget { MaxInlineListItems = 2 });

        var expanded = Assert.IsAssignableFrom<IReadOnlyList<ReportTextPart>>(parts[parts.Count - 1].Data);
        Assert.Equal(
        [
            new ReportTextPart(ReportInlineType.Text, "Elements "),
            new ReportTextPart(ReportInlineType.Hyperlink, "1", links[0]),
            new ReportTextPart(ReportInlineType.Text, ", "),
            new ReportTextPart(ReportInlineType.Hyperlink, "2", links[1]),
            new ReportTextPart(ReportInlineType.Text, ", "),
            new ReportTextPart(ReportInlineType.Expander, "+3", expanded),
        ], parts);

        // The expanded list shows one item per line with a marker, and an identifier is not quoted.
        Assert.Equal(
        [
            new ReportTextPart(ReportInlineType.Text, Marker),
            new ReportTextPart(ReportInlineType.Hyperlink, "1", links[0]),
            new ReportTextPart(ReportInlineType.LineBreak, "\n"),
            new ReportTextPart(ReportInlineType.Text, Marker),
            new ReportTextPart(ReportInlineType.Hyperlink, "2", links[1]),
            new ReportTextPart(ReportInlineType.LineBreak, "\n"),
            new ReportTextPart(ReportInlineType.Text, Marker),
            new ReportTextPart(ReportInlineType.Hyperlink, "3", links[2]),
            new ReportTextPart(ReportInlineType.LineBreak, "\n"),
            new ReportTextPart(ReportInlineType.Text, Marker),
            new ReportTextPart(ReportInlineType.Hyperlink, "4", links[3]),
            new ReportTextPart(ReportInlineType.LineBreak, "\n"),
            new ReportTextPart(ReportInlineType.Text, Marker),
            new ReportTextPart(ReportInlineType.Hyperlink, "5", links[4]),
        ], expanded);
    }

    [Fact]
    public void Zero_list_budget_leaves_only_the_expander()
    {
        var links = Enumerable.Range(1, 3).Select(static number => new FakeLink(number.ToString())).ToArray();

        var parts = ReportMessageParser.ParseTemplateForDisplay(
            "Elements {ids}",
            new Dictionary<string, object> { ["ids"] = links },
            FakeLinks,
            new ReportDisplayBudget { MaxInlineListItems = 0 });

        var expanded = Assert.IsAssignableFrom<IReadOnlyList<ReportTextPart>>(parts[parts.Count - 1].Data);
        Assert.Equal(
        [
            new ReportTextPart(ReportInlineType.Text, "Elements "),
            new ReportTextPart(ReportInlineType.Expander, "+3", expanded),
        ], parts);
        Assert.Equal("• 1\n• 2\n• 3", ReportMessageParser.BuildPlainText(expanded));
    }

    [Fact]
    public void Long_name_list_is_folded_with_quoted_items_and_a_bulleted_list()
    {
        string[] names = ["Wall 1, level 1", "Wall 2", "Wall 3", "Wall 4"];

        var parts = ReportMessageParser.ParseTemplateForDisplay(
            "Names: {names}",
            new Dictionary<string, object> { ["names"] = names },
            linkProvider: null,
            new ReportDisplayBudget { MaxInlineListItems = 2 });

        var expanded = Assert.IsAssignableFrom<IReadOnlyList<ReportTextPart>>(parts[parts.Count - 1].Data);
        Assert.Equal(
        [
            new ReportTextPart(ReportInlineType.Text, "Names: "),
            new ReportTextPart(ReportInlineType.Text, OpenQuote),
            new ReportTextPart(ReportInlineType.Text, "Wall 1, level 1"),
            new ReportTextPart(ReportInlineType.Text, CloseQuote),
            new ReportTextPart(ReportInlineType.Text, ", "),
            new ReportTextPart(ReportInlineType.Text, OpenQuote),
            new ReportTextPart(ReportInlineType.Text, "Wall 2"),
            new ReportTextPart(ReportInlineType.Text, CloseQuote),
            new ReportTextPart(ReportInlineType.Text, ", "),
            new ReportTextPart(ReportInlineType.Expander, "+2", expanded),
        ], parts);

        Assert.Equal("• Wall 1, level 1\n• Wall 2\n• Wall 3\n• Wall 4", ReportMessageParser.BuildPlainText(expanded));
    }

    [Fact]
    public void Name_list_at_the_budget_is_inline_quoted_and_not_folded()
    {
        string[] names = ["Wall 1", "Wall 2", "Wall 3"];

        var parts = ReportMessageParser.ParseTemplateForDisplay(
            "Names: {names}",
            new Dictionary<string, object> { ["names"] = names },
            linkProvider: null,
            new ReportDisplayBudget { MaxInlineListItems = 3 });

        Assert.DoesNotContain(parts, static part => part.Type == ReportInlineType.Expander);
        Assert.Equal(
            "Names: “Wall 1”, “Wall 2”, “Wall 3”",
            ReportMessageParser.BuildPlainText(parts));
    }

    [Fact]
    public void Long_text_value_is_not_folded()
    {
        string details = string.Join(" ", Enumerable.Repeat("detail", 40));

        var parts = ReportMessageParser.ParseTemplateForDisplay(
            "Details: {details}",
            new Dictionary<string, object> { ["details"] = details },
            linkProvider: null,
            new ReportDisplayBudget { MaxInlineListItems = 1 });

        Assert.Equal(
        [
            new ReportTextPart(ReportInlineType.Text, "Details: "),
            new ReportTextPart(ReportInlineType.Text, details),
        ], parts);
    }

    [Fact]
    public void Multi_line_text_value_is_not_folded()
    {
        var parts = ReportMessageParser.ParseTemplateForDisplay(
            "Details: {details}",
            new Dictionary<string, object>
            {
                ["details"] = "Highlighted walls overlap.\r\nOne may be deleted.",
            },
            linkProvider: null,
            new ReportDisplayBudget());

        Assert.Equal(
        [
            new ReportTextPart(ReportInlineType.Text, "Details: "),
            new ReportTextPart(ReportInlineType.Text, "Highlighted walls overlap.\r\nOne may be deleted."),
        ], parts);
    }

    [Fact]
    public void Template_text_itself_is_never_folded()
    {
        string template = "A long sentence with no placeholder in it.";

        var parts = ReportMessageParser.ParseTemplateForDisplay(
            template,
            new Dictionary<string, object>(),
            linkProvider: null,
            new ReportDisplayBudget { MaxInlineListItems = 1 });

        Assert.Equal([new ReportTextPart(ReportInlineType.Text, template)], parts);
    }

    [Fact]
    public void Full_parse_keeps_every_link_of_the_list()
    {
        var links = Enumerable.Range(1, 5).Select(static number => new FakeLink(number.ToString())).ToArray();

        ReportMessage full = ReportMessageParser.Parse(
            "Elements {ids}",
            new Dictionary<string, object> { ["ids"] = links },
            FakeLinks);

        Assert.DoesNotContain(full.Parts, static part => part.Type == ReportInlineType.Expander);
        Assert.Equal("Elements 1, 2, 3, 4, 5", full.Text);
    }

    [Fact]
    public void Full_parse_joins_a_name_list_with_commas_and_keeps_the_names_unquoted()
    {
        string[] names = ["Wall 1, level 1", "Wall 2", "Wall 3"];

        ReportMessage full = ReportMessageParser.Parse(
            "Names: {names}",
            new Dictionary<string, object> { ["names"] = names });

        Assert.Equal("Names: Wall 1, level 1, Wall 2, Wall 3", full.Text);
    }

    [Fact]
    public void Display_parse_requires_a_budget()
    {
        Assert.Throws<ArgumentNullException>(() => ReportMessageParser.ParseTemplateForDisplay(
            "Element {id}",
            new Dictionary<string, object> { ["id"] = new FakeLink("1") },
            FakeLinks,
            budget: null!));
    }

    private static IReadOnlyList<ReportLink>? FakeLinks(object? value) => value switch
    {
        FakeLink link => [new(link.Text, link)],
        IEnumerable<FakeLink> links => links.Select(static link => new ReportLink(link.Text, link)).ToList(),
        _ => null,
    };

    private sealed record FakeLink(string Text);
}
