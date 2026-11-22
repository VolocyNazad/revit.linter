using System.Text;

namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>
/// A search box text split into plain text and exact tag names.
/// </summary>
/// <remarks>
/// A tag name starts with <c>#</c> at the beginning of the query or after whitespace and runs to the
/// next whitespace. A tag name that contains whitespace, <c>#</c>, a backslash or a double quote is
/// written in double quotes, for example <c>#"fire alarm"</c>; a backslash escapes the next character
/// inside the quotes. Every tag name must match a row tag exactly, ignoring case, and several tag
/// names narrow the list together. Text outside the tag names is matched as a substring of the code
/// or the message; <see cref="Options"/> decides whether the case and the word boundaries matter. A
/// <c>#</c> inside a word is plain text.
/// </remarks>
public sealed class DiagnosticSearchQuery
{
    /// <summary>Gets the empty query, which matches every row.</summary>
    public static DiagnosticSearchQuery Empty { get; } = new(string.Empty, Array.Empty<string>(), SearchMatchOptions.Default);

    private DiagnosticSearchQuery(string text, IReadOnlyList<string> tags, SearchMatchOptions options)
    {
        Text = text;
        Tags = tags;
        Options = options;
    }

    /// <summary>Gets the plain text searched in the code and the message.</summary>
    public string Text { get; }

    /// <summary>Gets the exact tag names typed with <c>#</c>.</summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>Gets how the plain text is compared with the code and the message.</summary>
    /// <remarks>Tag names always match a row tag exactly and ignore case, whatever these options say.</remarks>
    public SearchMatchOptions Options { get; }

    /// <summary>Gets whether the query has neither plain text nor tag names.</summary>
    public bool IsEmpty => Text.Length == 0 && Tags.Count == 0;

    /// <summary>
    /// Parses the text of a search box.
    /// </summary>
    /// <param name="searchText">The text typed in the search box, or <see langword="null"/>.</param>
    /// <param name="options">How the plain text is compared with the row text.</param>
    /// <returns>The parsed query; <see cref="Empty"/> when the text is empty or holds no terms.</returns>
    public static DiagnosticSearchQuery Parse(string? searchText, SearchMatchOptions options = default)
    {
        if (searchText is null || string.IsNullOrWhiteSpace(searchText)) return Empty;

        List<string> tags = [];
        StringBuilder text = new(searchText.Length);
        int position = 0;

        while (position < searchText.Length)
        {
            if (searchText[position] == '#'
                && (position == 0 || char.IsWhiteSpace(searchText[position - 1])))
            {
                position++;
                string tag = ReadTag(searchText, ref position);
                if (tag.Length > 0
                    && !tags.Any(existing => string.Equals(existing, tag, StringComparison.OrdinalIgnoreCase)))
                {
                    tags.Add(tag);
                }

                AppendSeparator(text);
                continue;
            }

            char character = searchText[position++];
            if (char.IsWhiteSpace(character))
                AppendSeparator(text);
            else
                text.Append(character);
        }

        string plainText = text.ToString().Trim();
        IReadOnlyList<string> tagList = tags.Count == 0 ? Array.Empty<string>() : tags.AsReadOnly();
        return plainText.Length == 0 && tags.Count == 0
            ? Empty
            : new DiagnosticSearchQuery(plainText, tagList, options);
    }

    /// <summary>
    /// Returns the search box text that searches for one tag.
    /// </summary>
    /// <param name="tag">The tag name, as it appears in the table.</param>
    /// <returns>The tag with a <c>#</c> prefix; quoted when its name needs quoting.</returns>
    public static string FormatTag(string tag)
    {
        string trimmed = tag.Trim();
        if (trimmed.Length == 0) return string.Empty;

        bool needsQuotes = trimmed.Any(char.IsWhiteSpace)
            || trimmed.Contains('#')
            || trimmed.Contains('"')
            || trimmed.Contains('\\');
        if (!needsQuotes) return "#" + trimmed;

        string escaped = trimmed.Replace("\\", "\\\\").Replace("\"", "\\\"");
        return "#\"" + escaped + "\"";
    }

    /// <summary>
    /// Returns whether the row carries every tag of the query.
    /// </summary>
    /// <param name="tags">The tags of the row.</param>
    /// <returns><see langword="true"/> when every query tag is present; otherwise, <see langword="false"/>.</returns>
    public bool Matches(IReadOnlyList<string> tags)
        => Tags.All(tag => tags.Any(candidate => string.Equals(candidate, tag, StringComparison.OrdinalIgnoreCase)));

    /// <summary>
    /// Returns whether the query matches the row text and tags.
    /// </summary>
    /// <param name="code">The diagnostic code of the row, or <see langword="null"/>.</param>
    /// <param name="message">The description or message of the row, or <see langword="null"/>.</param>
    /// <param name="tags">The tags of the row.</param>
    /// <returns><see langword="true"/> when the text and every tag match; otherwise, <see langword="false"/>.</returns>
    public bool Matches(string? code, string? message, IReadOnlyList<string> tags)
        => MatchesText(code, message) && Matches(tags);

    /// <summary>
    /// Returns whether the plain text occurs in the code or the message.
    /// </summary>
    /// <param name="code">The diagnostic code of the row, or <see langword="null"/>.</param>
    /// <param name="message">The description or message of the row, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the query has no plain text or the text occurs; otherwise, <see langword="false"/>.</returns>
    public bool MatchesText(string? code, string? message)
    {
        if (Text.Length == 0) return true;

        return SearchTextMatcher.Contains(code, Text, Options)
            || SearchTextMatcher.Contains(message, Text, Options);
    }

    private static string ReadTag(string source, ref int position)
    {
        if (position < source.Length && source[position] == '"')
        {
            position++;
            StringBuilder tag = new();
            while (position < source.Length)
            {
                char character = source[position++];
                if (character == '\\' && position < source.Length)
                {
                    tag.Append(source[position++]);
                    continue;
                }

                if (character == '"') break;
                tag.Append(character);
            }

            return tag.ToString().Trim();
        }

        int start = position;
        while (position < source.Length && !char.IsWhiteSpace(source[position])) position++;
        return source.Substring(start, position - start).Trim();
    }

    private static void AppendSeparator(StringBuilder text)
    {
        if (text.Length > 0 && text[text.Length - 1] != ' ') text.Append(' ');
    }
}
