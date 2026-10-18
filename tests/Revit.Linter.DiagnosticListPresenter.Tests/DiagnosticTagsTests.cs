using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.DiagnosticListPresenter.Tests;

/// <summary>
/// Covers the tag rules shared by the diagnostics pane, the reports pane and the exported report:
/// how tags are normalized on the way in, how system tags stay separate from user tags, and how tags
/// match the search text.
/// </summary>
public sealed class DiagnosticTagsTests
{
    [Fact]
    public void Normalize_returns_no_tags_for_null()
        => Assert.Empty(DiagnosticTags.Normalize(null));

    [Fact]
    public void Normalize_trims_and_drops_blank_tags()
        => Assert.Equal(["Project"], DiagnosticTags.Normalize(["  Project  ", "", "   "]));

    [Fact]
    public void Normalize_removes_duplicates_ignoring_case_and_keeps_the_first_spelling()
        => Assert.Equal(["Project", "Family"], DiagnosticTags.Normalize(["Project", "project", "Family", "PROJECT"]));

    [Fact]
    public void Normalize_keeps_the_declaration_order()
        => Assert.Equal(["Family", "Project"], DiagnosticTags.Normalize(["Family", "Project"]));

    [Fact]
    public void Normalize_returns_the_shared_empty_list_when_nothing_remains()
        => Assert.Same(DiagnosticTags.None, DiagnosticTags.Normalize([" ", null!, ""]));

    [Fact]
    public void Matches_is_false_when_the_diagnostic_has_no_tags()
        => Assert.False(DiagnosticTags.Matches(DiagnosticTags.None, "Project"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Matches_is_false_for_empty_search_text(string? searchText)
        => Assert.False(DiagnosticTags.Matches(["Project"], searchText));

    [Theory]
    [InlineData("Project")]
    [InlineData("project")]
    [InlineData("PROJECT")]
    public void Matches_compares_tags_ignoring_case(string searchText)
        => Assert.True(DiagnosticTags.Matches(["Project"], searchText));

    [Fact]
    public void Matches_finds_a_substring_of_a_tag()
        => Assert.True(DiagnosticTags.Matches(["Structural"], "truc"));

    [Fact]
    public void Matches_is_false_when_no_tag_contains_the_search_text()
        => Assert.False(DiagnosticTags.Matches(["Project", "Family"], "Mep"));

    [Fact]
    public void Matches_finds_a_tag_that_is_not_the_first_one()
        => Assert.True(DiagnosticTags.Matches(["Project", "Family"], "Family"));

    [Fact]
    public void Built_in_tags_are_the_expected_english_literals()
    {
        Assert.Equal("Project", DiagnosticTags.Project);
        Assert.Equal("Family", DiagnosticTags.Family);
        Assert.Equal("BuiltIn", DiagnosticTags.BuiltIn);
        Assert.Equal("Custom", DiagnosticTags.Custom);
        Assert.Equal("Collision", DiagnosticTags.Collision);
        Assert.Equal("ParameterProject", DiagnosticTags.ParameterProject);
        Assert.Equal("Example", DiagnosticTags.Example);
        Assert.Equal("Tour", DiagnosticTags.Tour);
        Assert.Equal("Debug", DiagnosticTags.Debug);
    }

    [Theory]
    [InlineData("builtin")]
    [InlineData("custom")]
    [InlineData("COLLISION")]
    [InlineData("ParameterProject")]
    [InlineData("  EXAMPLE  ")]
    [InlineData("tour")]
    [InlineData("DEBUG")]
    public void IsReserved_matches_a_system_tag_ignoring_case_and_surrounding_whitespace(string tag)
        => Assert.True(DiagnosticTags.IsReserved(tag));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Project")]
    [InlineData("family")]
    [InlineData("Walls")]
    public void IsReserved_is_false_for_the_document_kinds_and_other_tags(string? tag)
        => Assert.False(DiagnosticTags.IsReserved(tag));

    [Fact]
    public void ForConfiguredRule_declares_the_source_then_the_provenance_in_order()
        => Assert.Equal(
            [DiagnosticTags.Collision, DiagnosticTags.Tour],
            DiagnosticTags.ForConfiguredRule(DiagnosticTags.Collision, isExample: false, isTour: true, isDebug: false));

    [Fact]
    public void ForConfiguredRule_adds_every_provenance_tag_that_is_set()
        => Assert.Equal(
            [DiagnosticTags.Custom, DiagnosticTags.Example, DiagnosticTags.Debug],
            DiagnosticTags.ForConfiguredRule(DiagnosticTags.Custom, isExample: true, isTour: false, isDebug: true));

    [Fact]
    public void ForConfiguredRule_lists_only_the_source_for_a_plain_rule()
        => Assert.Equal(
            [DiagnosticTags.ParameterProject],
            DiagnosticTags.ForConfiguredRule(DiagnosticTags.ParameterProject, isExample: false, isTour: false, isDebug: false));

    [Fact]
    public void Merge_puts_system_tags_before_user_tags()
        => Assert.Equal(
            [DiagnosticTags.Project, "Walls"],
            DiagnosticTags.Merge([DiagnosticTags.Project], ["Walls"]));

    [Fact]
    public void Merge_drops_reserved_names_from_the_user_tags()
        => Assert.Equal([DiagnosticTags.Custom, "Walls"], DiagnosticTags.Merge([DiagnosticTags.Custom], ["Tour", "tour", "Walls"]));

    [Fact]
    public void Merge_keeps_the_document_kind_tags_a_rule_declares_itself()
        => Assert.Equal(
            [DiagnosticTags.Custom, DiagnosticTags.Project, DiagnosticTags.Family, "Walls"],
            DiagnosticTags.Merge([DiagnosticTags.Custom], ["Project", "Family", "Walls"]));

    [Fact]
    public void Merge_deduplicates_user_tags_and_ignores_case()
        => Assert.Equal(["Walls"], DiagnosticTags.Merge(null, ["Walls", "walls"]));

    [Fact]
    public void Merge_returns_the_shared_empty_list_when_nothing_remains()
        => Assert.Same(DiagnosticTags.None, DiagnosticTags.Merge(null, null));

    [Fact]
    public void ElementDiagnosticId_keeps_system_tags_before_user_tags()
    {
        ElementDiagnosticId identity = new(
            "X001", "description", "message",
            DiagnosticSeverity.Warning, isActive: true, isObsolete: false, obsoleteDescription: string.Empty,
            systemTags: [DiagnosticTags.Project, DiagnosticTags.Project],
            tags: [" Naming ", "naming", "Walls"]);

        Assert.Equal([DiagnosticTags.Project], identity.SystemTags);
        Assert.Equal([DiagnosticTags.Project, "Naming", "Walls"], identity.Tags);
    }

    [Fact]
    public void User_tags_cannot_reuse_a_reserved_system_tag_name()
    {
        ElementDiagnosticId identity = new(
            "X003", "description", "message",
            DiagnosticSeverity.Warning, isActive: true, isObsolete: false, obsoleteDescription: string.Empty,
            tags: ["Tour", "tour", "Collision", "Walls"]);

        Assert.Empty(identity.SystemTags);
        Assert.Equal(["Walls"], identity.Tags);
    }

    [Fact]
    public void DocumentDiagnosticId_has_no_tags_by_default()
    {
        DocumentDiagnosticId identity = new(
            "X002", "description", "message",
            DiagnosticSeverity.Message, isActive: true, isObsolete: false, obsoleteDescription: string.Empty);

        Assert.Empty(identity.SystemTags);
        Assert.Empty(identity.Tags);
    }
}
