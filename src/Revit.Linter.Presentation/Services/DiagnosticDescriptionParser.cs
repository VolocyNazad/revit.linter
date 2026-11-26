using Revit.Linter.ReportMessaging;

namespace Revit.Linter.Presentation.Services;

/// <summary>
/// Splits a diagnostic description into text and links.
/// </summary>
/// <remarks>
/// A link is written as <c>[text](url)</c> inside the description, so a sentence carries the link where
/// the author puts it and any number of links. Only an absolute <c>http</c> or <c>https</c> address
/// becomes a link: any other markup stays visible as text, so a broken link never hides the sentence.
/// </remarks>
public static class DiagnosticDescriptionParser
{
    /// <summary>Parses a description into the parts the diagnostic list renders.</summary>
    /// <param name="description">The localized description.</param>
    /// <returns>The ordered text and link parts; empty for an empty description.</returns>
    public static IReadOnlyList<ReportTextPart> Parse(string? description)
    {
        if (string.IsNullOrEmpty(description)) return [];

        List<ReportTextPart> parts = [];
        int pendingStart = 0;
        int searchStart = 0;

        while (searchStart < description.Length)
        {
            int open = description.IndexOf('[', searchStart);
            if (open < 0) break;

            int close = description.IndexOf(']', open + 1);
            if (close < 0 || close + 1 >= description.Length || description[close + 1] != '(')
            {
                // The bracket does not start a link: keep scanning after it, the text stays as it is.
                searchStart = open + 1;
                continue;
            }

            int end = FindClosingParenthesis(description, close + 2);
            if (end < 0)
            {
                searchStart = open + 1;
                continue;
            }

            string text = description.Substring(open + 1, close - open - 1).Trim();
            string url = description.Substring(close + 2, end - close - 2).Trim();
            if (text.Length == 0 || !IsWebAddress(url))
            {
                searchStart = open + 1;
                continue;
            }

            AddText(parts, description.Substring(pendingStart, open - pendingStart));
            parts.Add(new(ReportInlineType.Hyperlink, text, url));
            pendingStart = end + 1;
            searchStart = pendingStart;
        }

        AddText(parts, description.Substring(pendingStart));
        return parts;
    }

    /// <summary>Gets whether a link may open the address.</summary>
    /// <param name="url">The address written in the description.</param>
    /// <returns><see langword="true"/> for an absolute <c>http</c> or <c>https</c> address.</returns>
    public static bool IsWebAddress(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    // An address may itself contain parentheses, so the link ends at the one that closes the first.
    private static int FindClosingParenthesis(string text, int start)
    {
        int depth = 1;
        for (int index = start; index < text.Length; index++)
        {
            if (text[index] == '(') depth++;
            else if (text[index] == ')' && --depth == 0) return index;
        }
        return -1;
    }

    private static void AddText(List<ReportTextPart> parts, string text)
    {
        if (text.Length > 0) parts.Add(new(ReportInlineType.Text, text));
    }
}
