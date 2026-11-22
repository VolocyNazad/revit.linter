namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>
/// Compares the text of a search box with the text of a row.
/// </summary>
public static class SearchTextMatcher
{
    /// <summary>
    /// Returns whether the search text occurs in the row text.
    /// </summary>
    /// <param name="source">The row text, or <see langword="null"/>.</param>
    /// <param name="text">The text typed in the search box.</param>
    /// <param name="options">How the two texts are compared.</param>
    /// <returns>
    /// <see langword="true"/> when the search text is empty or occurs in the row text as
    /// <paramref name="options"/> requires; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool Contains(string? source, string text, SearchMatchOptions options)
    {
        if (source is null) return false;
        if (text.Length == 0) return true;

        StringComparison comparison = options.MatchCase
            ? StringComparison.CurrentCulture
            : StringComparison.CurrentCultureIgnoreCase;
        if (!options.MatchWholeWord) return source.IndexOf(text, comparison) >= 0;

        return ContainsWholeWord(source, text, comparison);
    }

    private static bool ContainsWholeWord(string source, string text, StringComparison comparison)
    {
        bool textStartsOnBoundary = !IsWordCharacter(text[0]);
        bool textEndsOnBoundary = !IsWordCharacter(text[text.Length - 1]);
        int start = 0;

        while (start <= source.Length - text.Length)
        {
            int index = source.IndexOf(text, start, comparison);
            if (index < 0) return false;

            int end = index + text.Length;
            bool startsOnBoundary = textStartsOnBoundary
                || index == 0
                || !IsWordCharacter(source[index - 1]);
            bool endsOnBoundary = textEndsOnBoundary
                || end == source.Length
                || !IsWordCharacter(source[end]);
            if (startsOnBoundary && endsOnBoundary) return true;

            start = index + 1;
        }

        return false;
    }

    private static bool IsWordCharacter(char character)
        => char.IsLetterOrDigit(character) || character == '_';
}
