using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.DiagnosticListPresenter.Tests;

/// <summary>
/// Covers the search box syntax that both the diagnostics pane and the reports pane use: plain text
/// searches the code and the message, and a name typed with <c>#</c> searches an exact tag.
/// </summary>
public sealed class DiagnosticSearchQueryTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("#")]
    public void Parse_returns_the_empty_query_for_text_without_terms(string? searchText)
    {
        DiagnosticSearchQuery query = DiagnosticSearchQuery.Parse(searchText);

        Assert.True(query.IsEmpty);
        Assert.Empty(query.Text);
        Assert.Empty(query.Tags);
    }

    [Fact]
    public void Parse_keeps_plain_text_and_reads_no_tags()
    {
        DiagnosticSearchQuery query = DiagnosticSearchQuery.Parse("wall top");

        Assert.Equal("wall top", query.Text);
        Assert.Empty(query.Tags);
    }

    [Fact]
    public void Parse_reads_a_tag_after_the_hash()
    {
        DiagnosticSearchQuery query = DiagnosticSearchQuery.Parse("#Project");

        Assert.Empty(query.Text);
        Assert.Equal(["Project"], query.Tags);
    }

    [Fact]
    public void Parse_reads_several_tags_and_plain_text()
    {
        DiagnosticSearchQuery query = DiagnosticSearchQuery.Parse("wall #Project #Family");

        Assert.Equal("wall", query.Text);
        Assert.Equal(["Project", "Family"], query.Tags);
    }

    [Fact]
    public void Parse_ignores_duplicate_tags()
    {
        DiagnosticSearchQuery query = DiagnosticSearchQuery.Parse("#Project #project");

        Assert.Equal(["Project"], query.Tags);
    }

    [Fact]
    public void Parse_supports_a_tag_name_with_spaces_in_quotes()
    {
        DiagnosticSearchQuery query = DiagnosticSearchQuery.Parse("#\"fire alarm\"");

        Assert.Empty(query.Text);
        Assert.Equal(["fire alarm"], query.Tags);
    }

    [Fact]
    public void Parse_supports_an_escaped_quote_inside_a_tag_name()
    {
        DiagnosticSearchQuery query = DiagnosticSearchQuery.Parse("#\"say \\\"hi\\\"\"");

        Assert.Equal(["say \"hi\""], query.Tags);
    }

    [Fact]
    public void Parse_keeps_a_hash_inside_a_word_as_plain_text()
    {
        DiagnosticSearchQuery query = DiagnosticSearchQuery.Parse("C#");

        Assert.Equal("C#", query.Text);
        Assert.Empty(query.Tags);
    }

    [Fact]
    public void Tag_matching_is_exact_and_ignores_case()
    {
        DiagnosticSearchQuery query = DiagnosticSearchQuery.Parse("#project");

        Assert.True(query.Matches(["Project", "Family"]));
        Assert.False(query.Matches(["ProjectDocs"]));
    }

    [Fact]
    public void Every_tag_narrows_the_result()
    {
        DiagnosticSearchQuery query = DiagnosticSearchQuery.Parse("#Project #Family");

        Assert.True(query.Matches(["Project", "Family", "Walls"]));
        Assert.False(query.Matches(["Project", "Walls"]));
    }

    [Fact]
    public void Plain_text_does_not_match_a_tag_name()
    {
        DiagnosticSearchQuery query = DiagnosticSearchQuery.Parse("Project");

        Assert.False(query.Matches("SHRD001", "The wall is not connected", ["Project"]));
        Assert.True(query.Matches("SHRD001", "The Project wall is not connected", ["Family"]));
    }

    [Fact]
    public void A_tag_query_does_not_match_the_message_text()
    {
        DiagnosticSearchQuery query = DiagnosticSearchQuery.Parse("#Project");

        Assert.False(query.Matches("SHRD001", "The Project wall is not connected", ["Family"]));
    }

    [Fact]
    public void Plain_text_and_a_tag_must_both_match()
    {
        DiagnosticSearchQuery query = DiagnosticSearchQuery.Parse("wall #Project");

        Assert.True(query.Matches("SHRD001", "The wall is not connected", ["Project"]));
        Assert.False(query.Matches("SHRD001", "The floor is not connected", ["Project"]));
        Assert.False(query.Matches("SHRD001", "The wall is not connected", ["Family"]));
    }

    [Theory]
    [InlineData("Project", "#Project")]
    [InlineData("fire alarm", "#\"fire alarm\"")]
    public void FormatTag_writes_a_tag_that_parses_back(string tag, string expected)
    {
        string formatted = DiagnosticSearchQuery.FormatTag(tag);

        Assert.Equal(expected, formatted);
        Assert.Equal([tag], DiagnosticSearchQuery.Parse(formatted).Tags);
    }

    [Fact]
    public void Parse_keeps_the_match_options()
    {
        DiagnosticSearchQuery query = DiagnosticSearchQuery.Parse("wall", new SearchMatchOptions(true, true));

        Assert.Equal(new SearchMatchOptions(true, true), query.Options);
    }

    [Fact]
    public void Plain_text_ignores_case_and_matches_part_of_a_word_by_default()
    {
        DiagnosticSearchQuery query = DiagnosticSearchQuery.Parse("wall");

        Assert.True(query.MatchesText("SHRD001", "The Wallboard is not connected"));
        Assert.True(query.MatchesText("SHRD001", "The wall is not connected"));
    }

    [Fact]
    public void Match_case_requires_the_same_letters()
    {
        DiagnosticSearchQuery query = DiagnosticSearchQuery.Parse("Wall", new SearchMatchOptions(true, false));

        Assert.True(query.MatchesText("SHRD001", "The Wall is not connected"));
        Assert.False(query.MatchesText("SHRD001", "The wall is not connected"));
    }

    [Fact]
    public void Match_case_applies_to_the_code_too()
    {
        DiagnosticSearchQuery query = DiagnosticSearchQuery.Parse("shrd001", new SearchMatchOptions(true, false));

        Assert.False(query.MatchesText("SHRD001", "The wall is not connected"));
    }

    [Fact]
    public void Whole_word_skips_a_part_of_a_longer_word()
    {
        DiagnosticSearchQuery query = DiagnosticSearchQuery.Parse("wall", new SearchMatchOptions(false, true));

        Assert.False(query.MatchesText("SHRD001", "The wallboard is not connected"));
        Assert.True(query.MatchesText("SHRD001", "The wall is not connected"));
    }

    [Fact]
    public void Whole_word_accepts_punctuation_and_the_text_end_as_a_boundary()
    {
        DiagnosticSearchQuery query = DiagnosticSearchQuery.Parse("wall", new SearchMatchOptions(false, true));

        Assert.True(query.MatchesText("SHRD001", "(wall) is not connected"));
        Assert.True(query.MatchesText("SHRD001", "The wall"));
        Assert.False(query.MatchesText("SHRD001", "The firewall"));
    }

    [Fact]
    public void Whole_word_and_match_case_work_together()
    {
        DiagnosticSearchQuery query = DiagnosticSearchQuery.Parse("Wall", new SearchMatchOptions(true, true));

        Assert.True(query.MatchesText("SHRD001", "The Wall is not connected"));
        Assert.False(query.MatchesText("SHRD001", "The wall is not connected"));
        Assert.False(query.MatchesText("SHRD001", "The Wallboard is not connected"));
    }

    [Fact]
    public void Match_options_do_not_affect_tags()
    {
        DiagnosticSearchQuery query = DiagnosticSearchQuery.Parse("#project", new SearchMatchOptions(true, true));

        Assert.True(query.Matches(["Project"]));
        Assert.True(query.Matches(["Project", "Family"]));
    }
}
