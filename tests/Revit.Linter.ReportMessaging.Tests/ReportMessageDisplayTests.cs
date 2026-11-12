namespace Revit.Linter.ReportMessaging.Tests;

public sealed class ReportMessageDisplayTests
{
    [Fact]
    public void Short_message_is_not_folded()
    {
        ReportMessageDisplay display = new(
            "Element {elementId} is not pinned.",
            new Dictionary<string, object> { ["elementId"] = "1234567" },
            linkProvider: null,
            new ReportDisplayBudget());

        Assert.DoesNotContain(display.Parts, static part => part.Type == ReportInlineType.Expander);
        Assert.Equal(display.Text, ReportMessageParser.BuildPlainText(display.Parts));
    }

    [Fact]
    public void Long_list_is_folded_in_the_display_parts_only()
    {
        string[] names = ["Wall 1", "Wall 2", "Wall 3", "Wall 4", "Wall 5"];
        ReportMessageDisplay display = new(
            "Elements: {names}",
            new Dictionary<string, object> { ["names"] = names },
            linkProvider: null,
            new ReportDisplayBudget { MaxInlineListItems = 2 });

        Assert.Contains(display.Parts, static part => part.Type == ReportInlineType.Expander);
        Assert.Equal("Elements: Wall 1, Wall 2, Wall 3, Wall 4, Wall 5", display.Text);
        Assert.Contains("\u201CWall 1\u201D", ReportMessageParser.BuildPlainText(display.Parts));
        Assert.DoesNotContain("Wall 5", ReportMessageParser.BuildPlainText(display.Parts));
    }

    [Fact]
    public void Full_message_matches_a_full_parse()
    {
        var links = new[] { new FakeLink("1"), new FakeLink("2"), new FakeLink("3"), new FakeLink("4") };
        var args = new Dictionary<string, object> { ["ids"] = links };
        ReportMessageDisplay display = new(
            "Elements {ids}",
            args,
            FakeLinks,
            new ReportDisplayBudget { MaxInlineListItems = 2 });

        ReportMessage full = ReportMessageParser.Parse("Elements {ids}", args, FakeLinks);

        Assert.Equal(full.Text, display.Text);
        Assert.Equal(full.Parts, display.FullMessage.Parts);
        Assert.DoesNotContain(display.FullMessage.Parts, static part => part.Type == ReportInlineType.Expander);
        Assert.Equal(2, display.Parts.Count(static part => part.Type == ReportInlineType.Hyperlink));
    }

    [Fact]
    public void Display_requires_a_template()
    {
        Assert.Throws<ArgumentNullException>(() => new ReportMessageDisplay(
            template: null!,
            new Dictionary<string, object>(),
            linkProvider: null,
            new ReportDisplayBudget()));
    }

    private static IReadOnlyList<ReportLink>? FakeLinks(object? value) => value switch
    {
        FakeLink link => [new(link.Text, link)],
        IEnumerable<FakeLink> links => links.Select(static link => new ReportLink(link.Text, link)).ToList(),
        _ => null,
    };

    private sealed record FakeLink(string Text);
}
