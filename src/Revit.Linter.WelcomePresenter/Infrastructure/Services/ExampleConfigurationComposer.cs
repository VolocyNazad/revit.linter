using System.Text;
using System.Text.RegularExpressions;

namespace Revit.Linter.WelcomePresenter.Infrastructure.Services;

/// <summary>
/// Builds an installable configuration file from an example template.
/// </summary>
internal static class ExampleConfigurationComposer
{
    private static readonly Regex SectionStart = new(
        @"^#\s*>>>\s*section:\s*(?<name>[A-Za-z]+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex SectionEnd = new(
        @"^#\s*<<<\s*section:", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ParameterGroup = new(
        @"autodesk\.parameter\.group:(?<name>[A-Za-z]+)-\d+\.\d+\.\d+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // Revit 2021-2023 identify a parameter group by its BuiltInParameterGroup name.
    private static readonly Dictionary<string, string> LegacyParameterGroups = new(StringComparer.Ordinal)
    {
        ["data"] = "PG_DATA",
        ["text"] = "PG_TEXT",
        ["identityData"] = "PG_IDENTITY_DATA",
        ["general"] = "PG_GENERAL",
    };

    /// <summary>
    /// Keeps the lines outside any section and the sections that were selected, and optionally rewrites
    /// parameter groups for Revit versions before 2024.
    /// </summary>
    /// <param name="template">The example template text.</param>
    /// <param name="sections">The names of the sections to keep, compared case-insensitively.</param>
    /// <param name="useLegacyParameterGroups">
    /// Whether known Revit 2024+ parameter group identifiers are replaced by <c>PG_*</c> names.
    /// </param>
    /// <returns>The composed configuration text with <c>\n</c> line endings normalized to the platform's.</returns>
    /// <remarks>
    /// A section starts at a <c># &gt;&gt;&gt; section: name</c> comment line and ends at the next
    /// <c># &lt;&lt;&lt; section:</c> line. A template without sections is returned whole. Consecutive blank
    /// lines left by a removed section are collapsed into one. An unknown group identifier is kept as is.
    /// </remarks>
    public static string Compose(string template, IReadOnlyCollection<string> sections, bool useLegacyParameterGroups)
    {
        HashSet<string> selected = new(sections, StringComparer.OrdinalIgnoreCase);
        StringBuilder result = new(template.Length);
        bool skipping = false;
        bool previousBlank = true;

        foreach (string rawLine in template.Replace("\r\n", "\n").Split('\n'))
        {
            Match start = SectionStart.Match(rawLine);
            if (start.Success)
            {
                skipping = !selected.Contains(start.Groups["name"].Value);
                if (skipping) continue;
            }
            else if (SectionEnd.IsMatch(rawLine))
            {
                bool wasSkipping = skipping;
                skipping = false;
                if (wasSkipping) continue;
            }
            else if (skipping)
            {
                continue;
            }

            bool blank = string.IsNullOrWhiteSpace(rawLine);
            if (blank && previousBlank) continue;
            previousBlank = blank;

            string line = useLegacyParameterGroups ? ToLegacyParameterGroups(rawLine) : rawLine;
            result.Append(line).Append(Environment.NewLine);
        }

        // Splitting leaves a trailing empty entry; keep exactly one final line break.
        return result.ToString().TrimEnd('\r', '\n') + Environment.NewLine;
    }

    private static string ToLegacyParameterGroups(string line) =>
        ParameterGroup.Replace(line, match =>
            LegacyParameterGroups.TryGetValue(match.Groups["name"].Value, out string? legacyName)
                ? legacyName
                : match.Value);
}
