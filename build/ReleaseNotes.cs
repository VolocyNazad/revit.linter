using System.Text;

namespace Revit.Linter.Build;

/// <summary>Builds the GitHub release description from the released section of <c>CHANGELOG.md</c>.</summary>
internal static class ReleaseNotes
{
    /// <summary>Extracts the changelog section for <paramref name="version"/> as Markdown release notes.</summary>
    /// <param name="changelogPath">The path to <c>CHANGELOG.md</c>.</param>
    /// <param name="version">The released version without the <c>v</c> prefix.</param>
    /// <param name="repositoryUrl">
    /// The repository web URL used for the link to the full changelog, or <see langword="null"/> to omit the link.
    /// </param>
    /// <returns>The section body without its version heading, followed by the optional changelog link.</returns>
    /// <exception cref="InvalidOperationException">The version has no section, or the section is empty.</exception>
    /// <remarks>
    /// The section spans from the <c>## [version]</c> heading to the next second-level heading. The link targets
    /// the heading anchor in the changelog at the release tag, so it keeps showing the released text.
    /// </remarks>
    public static string Create(string changelogPath, string version, string? repositoryUrl)
    {
        string[] lines = File.ReadAllLines(changelogPath);
        string headingPrefix = $"## [{version}]";
        int start = Array.FindIndex(lines, line => line.StartsWith(headingPrefix, StringComparison.Ordinal));
        if (start < 0)
            throw new InvalidOperationException($"CHANGELOG.md has no '{headingPrefix}' section.");

        int end = Array.FindIndex(lines, start + 1, line => line.StartsWith("## ", StringComparison.Ordinal));
        if (end < 0)
            end = lines.Length;

        string body = string.Join('\n', lines[(start + 1)..end]).Trim();
        if (body.Length == 0)
            throw new InvalidOperationException($"The '{headingPrefix}' section of CHANGELOG.md is empty.");

        if (string.IsNullOrWhiteSpace(repositoryUrl))
            return body + "\n";

        string anchor = CreateHeadingAnchor(lines[start][3..]);
        return $"{body}\n\n**Full changelog:** {repositoryUrl}/blob/v{version}/CHANGELOG.md#{anchor}\n";
    }

    // Mirrors GitHub's heading anchors: lower-case, punctuation removed, each space replaced by a hyphen.
    private static string CreateHeadingAnchor(string heading)
    {
        var anchor = new StringBuilder(heading.Length);
        foreach (char character in heading.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character) || character is '-' or '_')
                anchor.Append(character);
            else if (character == ' ')
                anchor.Append('-');
        }

        return anchor.ToString();
    }
}
