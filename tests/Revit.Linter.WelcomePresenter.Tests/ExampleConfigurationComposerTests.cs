using Revit.Linter.WelcomePresenter.Infrastructure.Services;

namespace Revit.Linter.WelcomePresenter.Tests;

public sealed class ExampleConfigurationComposerTests
{
    private const string Template = """
        # header

        # >>> section: mep — MEP systems
        - code: "A"
          group: "autodesk.parameter.group:data-1.0.0"
        # <<< section: mep

        # >>> section: structure — Structure
        - code: "B"
          group: "autodesk.parameter.group:unknownGroup-1.0.0"
        # <<< section: structure
        """;

    [Fact]
    public void Keeps_only_the_selected_sections_and_the_lines_outside_sections()
    {
        string[] lines = Compose(Template, ["structure"]);

        string[] expected =
        [
            "# header",
            "",
            "# >>> section: structure — Structure",
            "- code: \"B\"",
            "  group: \"autodesk.parameter.group:unknownGroup-1.0.0\"",
            "# <<< section: structure",
        ];
        Assert.Equal(expected, lines);
    }

    [Fact]
    public void Section_names_are_compared_case_insensitively()
    {
        string[] lines = Compose(Template, ["MEP"]);

        Assert.Contains("- code: \"A\"", lines);
        Assert.DoesNotContain("- code: \"B\"", lines);
    }

    [Fact]
    public void Template_without_sections_is_kept_whole()
    {
        string[] lines = Compose("- code: \"A\"\n\n- code: \"B\"\n", []);

        Assert.Equal(new[] { "- code: \"A\"", "", "- code: \"B\"" }, lines);
    }

    [Fact]
    public void Blank_lines_left_by_a_removed_section_are_collapsed()
    {
        string[] lines = Compose("# header\n\n# >>> section: mep\n- code: \"A\"\n# <<< section: mep\n\n- code: \"C\"\n", []);

        Assert.Equal(new[] { "# header", "", "- code: \"C\"" }, lines);
    }

    [Fact]
    public void Result_ends_with_exactly_one_line_break()
    {
        string result = ExampleConfigurationComposer.Compose("- code: \"A\"\r\n\r\n\r\n", [], false);

        Assert.Equal("- code: \"A\"" + Environment.NewLine, result);
    }

    [Fact]
    public void Known_parameter_groups_are_converted_for_revit_before_2024()
    {
        string[] lines = Compose(Template, ["mep", "structure"], useLegacyParameterGroups: true);

        Assert.Contains("  group: \"PG_DATA\"", lines);
        // An identifier without a known PG_* counterpart is left for the configuration validator to report.
        Assert.Contains("  group: \"autodesk.parameter.group:unknownGroup-1.0.0\"", lines);
    }

    [Fact]
    public void Parameter_groups_are_kept_for_revit_2024_and_newer()
    {
        string[] lines = Compose(Template, ["mep"]);

        Assert.Contains("  group: \"autodesk.parameter.group:data-1.0.0\"", lines);
    }

    private static string[] Compose(string template, string[] sections, bool useLegacyParameterGroups = false) =>
        ExampleConfigurationComposer.Compose(template, sections, useLegacyParameterGroups)
            .Replace("\r\n", "\n")
            .TrimEnd('\n')
            .Split('\n');
}
