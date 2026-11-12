using Revit.Linter.WelcomePresenter.Abstractions.Models;
using Revit.Linter.WelcomePresenter.Infrastructure.Services;

namespace Revit.Linter.WelcomePresenter.Tests;

public sealed class TemplateCheckComposerTests
{
    private const string Configuration = "version: 2\n\nrules:\n- type: \"custom\"\n  code: \"CSTM001\"\n";

    [Fact]
    public void Append_puts_the_block_after_the_last_rule()
    {
        string block = "- type: \"parameter\"\n  code: \"PRMTR401\"\n";

        string result = TemplateCheckComposer.Append(
            Configuration, [TemplateCheckPack.Architecture], _ => block);

        string[] lines = result.Split(["\r\n", "\n"], StringSplitOptions.None);
        Assert.Contains("  code: \"CSTM001\"", lines);
        Assert.Contains("- type: \"parameter\"", lines);
        Assert.True(
            Array.IndexOf(lines, "  code: \"CSTM001\"") < Array.IndexOf(lines, "- type: \"parameter\""),
            "The check block must follow the example rules.");
        Assert.EndsWith(Environment.NewLine, result, StringComparison.Ordinal);
        Assert.False(result.EndsWith(Environment.NewLine + Environment.NewLine, StringComparison.Ordinal));
    }

    [Fact]
    public void Append_keeps_the_selection_order_and_skips_duplicates()
    {
        string result = TemplateCheckComposer.Append(
            Configuration,
            [TemplateCheckPack.Electrical, TemplateCheckPack.Architecture, TemplateCheckPack.Electrical],
            pack => $"- type: \"parameter\"\n  code: \"{TemplateCheckComposer.GetCodeNumber(pack)}\"\n");

        Assert.Equal(1, CountOccurrences(result, "code: \"408\""));
        Assert.Equal(1, CountOccurrences(result, "code: \"401\""));
        Assert.True(result.IndexOf("code: \"408\"", StringComparison.Ordinal)
                    < result.IndexOf("code: \"401\"", StringComparison.Ordinal));
    }

    [Fact]
    public void Append_rejects_an_empty_block()
    {
        Assert.Throws<InvalidOperationException>(() => TemplateCheckComposer.Append(
            Configuration, [TemplateCheckPack.Architecture], _ => "  \n"));
    }

    [Fact]
    public void Append_normalizes_line_endings_and_keeps_one_final_break()
    {
        string result = TemplateCheckComposer.Append(
            "version: 2\r\n\r\nrules:\r\n",
            [TemplateCheckPack.Hvac],
            _ => "- type: \"parameter\"\r\n  code: \"405\"\r\n\r\n");

        Assert.DoesNotContain("\r\r", result, StringComparison.Ordinal);
        Assert.EndsWith(Environment.NewLine, result, StringComparison.Ordinal);
        Assert.False(result.EndsWith(Environment.NewLine + Environment.NewLine, StringComparison.Ordinal));
    }

    [Fact]
    public void Code_numbers_are_unique_and_file_names_follow_them()
    {
        int[] numbers = Enum.GetValues(typeof(TemplateCheckPack)).Cast<TemplateCheckPack>()
            .Select(TemplateCheckComposer.GetCodeNumber)
            .ToArray();
        Assert.Equal(numbers.Length, numbers.Distinct().Count());
        foreach (TemplateCheckPack pack in Enum.GetValues(typeof(TemplateCheckPack)).Cast<TemplateCheckPack>())
        {
            Assert.Equal(
                $"adsk-{TemplateCheckComposer.GetCodeNumber(pack)}.yaml",
                TemplateCheckComposer.GetFileName(pack));
        }
    }

    private static int CountOccurrences(string text, string value)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
