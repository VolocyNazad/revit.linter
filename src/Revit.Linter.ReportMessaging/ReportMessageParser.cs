using System.Text;

namespace Revit.Linter.ReportMessaging;

/// <summary>
/// Parses named placeholders in report messages into rich text parts.
/// </summary>
public static class ReportMessageParser
{
    private const char OpenBrace = '{';
    private const char CloseBrace = '}';
    private const string ItemSeparator = ", ";

    private static bool IsEscapedAt(string text, int index, char brace)
        => index + 1 < text.Length && text[index] == brace && text[index + 1] == brace;

    /// <summary>
    /// Replaces named placeholders with text or links while preserving escaped braces.
    /// </summary>
    /// <param name="text">The message template to parse.</param>
    /// <param name="args">The values keyed by placeholder name.</param>
    /// <param name="linkProvider">
    /// An optional function that converts a placeholder value into interactive links.
    /// </param>
    /// <returns>The ordered parts of the parsed message.</returns>
    public static IReadOnlyList<ReportTextPart> ParseTemplate(
        string text,
        IReadOnlyDictionary<string, object> args,
        Func<object?, IReadOnlyList<ReportLink>?>? linkProvider = null)
    {
        List<ReportTextPart> parts = [];
        StringBuilder literal = new();

        void FlushLiteral()
        {
            if (literal.Length > 0)
            {
                parts.Add(new(ReportInlineType.Text, literal.ToString()));
                literal.Clear();
            }
        }

        int ParsePlaceholder(int startIndex)
        {
            int endIndex = text.IndexOf(CloseBrace, startIndex + 1);
            if (endIndex == -1)
            {
                literal.Append(text.Substring(startIndex));
                return text.Length;
            }

            string key = text.Substring(startIndex + 1, endIndex - startIndex - 1);

            if (key.Length > 0 && args.TryGetValue(key, out object? value))
            {
                FlushLiteral();
                parts.AddRange(CreateTextPartsFromValue(value, linkProvider));
            }
            else
            {
                literal.Append(text.Substring(startIndex, endIndex - startIndex + 1));
            }

            return endIndex + 1;
        }

        int index = 0;
        while (index < text.Length)
        {
            char current = text[index];

            if (current == OpenBrace)
            {
                if (IsEscapedAt(text, index, OpenBrace))
                {
                    literal.Append(OpenBrace);
                    index += 2;
                    continue;
                }

                index = ParsePlaceholder(index);
            }
            else if (IsEscapedAt(text, index, CloseBrace))
            {
                literal.Append(CloseBrace);
                index += 2;
            }
            else
            {
                literal.Append(current);
                index++;
            }
        }

        FlushLiteral();

        return parts;
    }

    /// <summary>
    /// Parses a message template and creates both rich and plain-text representations.
    /// </summary>
    /// <param name="text">The message template to parse.</param>
    /// <param name="args">The values keyed by placeholder name.</param>
    /// <param name="linkProvider">
    /// An optional function that converts a placeholder value into interactive links.
    /// </param>
    /// <returns>The parsed report message.</returns>
    public static ReportMessage Parse(
        string text,
        IReadOnlyDictionary<string, object> args,
        Func<object?, IReadOnlyList<ReportLink>?>? linkProvider = null)
    {
        IReadOnlyList<ReportTextPart> parts = ParseTemplate(text, args, linkProvider);
        return new(parts, BuildPlainText(parts));
    }

    private static IEnumerable<ReportTextPart> CreateTextPartsFromValue(
        object? value,
        Func<object?, IReadOnlyList<ReportLink>?>? linkProvider = null)
    {
        List<ReportTextPart> parts = [];

        if (linkProvider?.Invoke(value) is { } links)
        {
            bool first = true;
            foreach (var link in links)
            {
                if (!first) parts.Add(new(ReportInlineType.Text, ItemSeparator));
                parts.Add(new(ReportInlineType.Hyperlink, link.Text, link.Data));
                first = false;
            }

            return parts;
        }

        parts.Add(new(ReportInlineType.Text, value?.ToString() ?? string.Empty));

        return parts;
    }

    /// <summary>
    /// Concatenates report parts into a plain-text message.
    /// </summary>
    /// <param name="parts">The ordered message parts.</param>
    /// <returns>The concatenated message text.</returns>
    public static string BuildPlainText(IEnumerable<ReportTextPart> parts)
    {
        StringBuilder builder = new();
        foreach (var part in parts) builder.Append(part.Text);
        return builder.ToString();
    }
}
