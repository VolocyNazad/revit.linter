namespace Revit.Linter.ReportMessaging.Tests;

public sealed class ReportMessageParserTests
{
    [Fact]
    public void Plain_text_without_placeholders_returns_single_run()
    {
        var parts = ReportMessageParser.ParseTemplate("No placeholders", new Dictionary<string, object>());

        var part = Assert.Single(parts);
        Assert.Equal(ReportInlineType.Text, part.Type);
        Assert.Equal("No placeholders", part.Text);
    }

    [Fact]
    public void Single_link_placeholder_returns_hyperlink_with_data()
    {
        var link = new FakeLink("123");
        var parts = ReportMessageParser.ParseTemplate(
            "Element {id} found",
            new Dictionary<string, object> { ["id"] = link },
            FakeLinks);

        Assert.Equal(
        [
            new ReportTextPart(ReportInlineType.Text, "Element "),
            new ReportTextPart(ReportInlineType.Hyperlink, "123", link),
            new ReportTextPart(ReportInlineType.Text, " found"),
        ], parts);
    }

    [Fact]
    public void Link_collection_is_joined_with_comma()
    {
        var first = new FakeLink("1");
        var second = new FakeLink("2");
        var parts = ReportMessageParser.ParseTemplate(
            "Elements {ids}",
            new Dictionary<string, object> { ["ids"] = new FakeLink[] { first, second } },
            FakeLinks);

        Assert.Equal(
        [
            new ReportTextPart(ReportInlineType.Text, "Elements "),
            new ReportTextPart(ReportInlineType.Hyperlink, "1", first),
            new ReportTextPart(ReportInlineType.Text, ", "),
            new ReportTextPart(ReportInlineType.Hyperlink, "2", second),
        ], parts);
    }

    [Fact]
    public void Empty_link_collection_produces_no_parts()
    {
        var parts = ReportMessageParser.ParseTemplate(
            "Elements {ids}.",
            new Dictionary<string, object> { ["ids"] = Array.Empty<FakeLink>() },
            FakeLinks);

        Assert.Equal(
        [
            new ReportTextPart(ReportInlineType.Text, "Elements "),
            new ReportTextPart(ReportInlineType.Text, "."),
        ], parts);
    }

    [Fact]
    public void Value_without_link_provider_returns_run()
    {
        var parts = ReportMessageParser.ParseTemplate(
            "Count {count}",
            new Dictionary<string, object> { ["count"] = 42 });

        Assert.Equal(
        [
            new ReportTextPart(ReportInlineType.Text, "Count "),
            new ReportTextPart(ReportInlineType.Text, "42"),
        ], parts);
    }

    [Fact]
    public void Missing_key_is_preserved_verbatim()
    {
        var parts = ReportMessageParser.ParseTemplate("Element {id}", new Dictionary<string, object>());

        Assert.Equal(
        [
            new ReportTextPart(ReportInlineType.Text, "Element {id}"),
        ], parts);
    }

    [Fact]
    public void Empty_placeholder_is_preserved_verbatim()
    {
        var parts = ReportMessageParser.ParseTemplate("Count {} items", new Dictionary<string, object>());

        Assert.Equal(
        [
            new ReportTextPart(ReportInlineType.Text, "Count {} items"),
        ], parts);
    }

    [Fact]
    public void Doubled_braces_produce_literal_braces_without_substitution()
    {
        var parts = ReportMessageParser.ParseTemplate(
            "Literal {{id}} and {{}}",
            new Dictionary<string, object> { ["id"] = new FakeLink("123") },
            FakeLinks);

        Assert.Equal(
        [
            new ReportTextPart(ReportInlineType.Text, "Literal {id} and {}"),
        ], parts);
    }

    [Fact]
    public void Unclosed_brace_is_preserved_verbatim()
    {
        var parts = ReportMessageParser.ParseTemplate("Element {id", new Dictionary<string, object>());

        Assert.Equal(
        [
            new ReportTextPart(ReportInlineType.Text, "Element {id"),
        ], parts);
    }

    [Fact]
    public void Stray_closing_brace_is_preserved_verbatim()
    {
        var parts = ReportMessageParser.ParseTemplate("Count } items", new Dictionary<string, object>());

        Assert.Equal(
        [
            new ReportTextPart(ReportInlineType.Text, "Count } items"),
        ], parts);
    }

    [Fact]
    public void Build_plain_text_joins_all_parts()
    {
        var parts = ReportMessageParser.ParseTemplate(
            "Element {id}",
            new Dictionary<string, object> { ["id"] = new FakeLink("42") },
            FakeLinks);

        Assert.Equal("Element 42", ReportMessageParser.BuildPlainText(parts));
    }

    [Fact]
    public void Parse_returns_parts_and_plain_text_together()
    {
        FakeLink link = new("42");

        ReportMessage message = ReportMessageParser.Parse(
            "Element {id}",
            new Dictionary<string, object> { ["id"] = link },
            FakeLinks);

        Assert.Equal("Element 42", message.Text);
        Assert.Equal(
        [
            new ReportTextPart(ReportInlineType.Text, "Element "),
            new ReportTextPart(ReportInlineType.Hyperlink, "42", link),
        ], message.Parts);
    }

    private static IReadOnlyList<ReportLink>? FakeLinks(object? value) => value switch
    {
        FakeLink link => [new(link.Text, link)],
        IEnumerable<FakeLink> links => links.Select(static link => new ReportLink(link.Text, link)).ToList(),
        _ => null,
    };

    private sealed record FakeLink(string Text);
}
