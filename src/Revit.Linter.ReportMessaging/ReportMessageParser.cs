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
    private const string FoldedListPrefix = "+";
    private const string ListItemMarker = "\u2022 ";
    private const string LineBreakText = "\n";

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
        => ParseTemplateCore(text, args, linkProvider, budget: null);

    /// <summary>
    /// Replaces named placeholders with text or links and folds every list value that exceeds
    /// <paramref name="budget"/> into its first items and an expander part.
    /// </summary>
    /// <remarks>
    /// Only a list value is folded: a list of elements rendered as links or a list of names rendered as
    /// text. Scalar text keeps its template form, so a long prose value such as a Revit warning
    /// description is not shortened. The expander carries the complete list, and the plain text built by
    /// <see cref="BuildPlainText"/> stays complete.
    /// </remarks>
    /// <param name="text">The message template to parse.</param>
    /// <param name="args">The values keyed by placeholder name.</param>
    /// <param name="linkProvider">
    /// An optional function that converts a placeholder value into interactive links.
    /// </param>
    /// <param name="budget">The limits applied to every list value.</param>
    /// <returns>The ordered display parts of the parsed message.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="budget"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<ReportTextPart> ParseTemplateForDisplay(
        string text,
        IReadOnlyDictionary<string, object> args,
        Func<object?, IReadOnlyList<ReportLink>?>? linkProvider,
        ReportDisplayBudget budget)
    {
        if (budget is null)
            throw new ArgumentNullException(nameof(budget));

        return ParseTemplateCore(text, args, linkProvider, budget);
    }

    private static IReadOnlyList<ReportTextPart> ParseTemplateCore(
        string text,
        IReadOnlyDictionary<string, object> args,
        Func<object?, IReadOnlyList<ReportLink>?>? linkProvider,
        ReportDisplayBudget? budget)
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
                parts.AddRange(CreateTextPartsFromValue(value, linkProvider, budget));
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
        Func<object?, IReadOnlyList<ReportLink>?>? linkProvider,
        ReportDisplayBudget? budget)
    {
        if (linkProvider?.Invoke(value) is { } links)
            return CreateListParts(
                links,
                static link => new(ReportInlineType.Hyperlink, link.Text, link.Data),
                budget,
                namesAreQuoted: false);

        if (value is string text)
            return [new(ReportInlineType.Text, text)];

        if (value is IEnumerable<string> items)
            return CreateListParts(
                items as IReadOnlyList<string> ?? items.ToArray(),
                static item => new(ReportInlineType.Text, item),
                budget,
                namesAreQuoted: true);

        return [new(ReportInlineType.Text, value?.ToString() ?? string.Empty)];
    }

    private static IReadOnlyList<ReportTextPart> CreateListParts<T>(
        IReadOnlyList<T> items,
        Func<T, ReportTextPart> createPart,
        ReportDisplayBudget? budget,
        bool namesAreQuoted)
    {
        ReportDisplayBudget? quotes = namesAreQuoted ? budget : null;

        if (budget is null)
            return BuildInlineListParts(items, createPart, items.Count, quotes);

        int visibleCount = Math.Min(items.Count, Math.Max(0, budget.MaxInlineListItems));
        if (visibleCount == items.Count)
            return BuildInlineListParts(items, createPart, items.Count, quotes);

        List<ReportTextPart> parts = [.. BuildInlineListParts(items, createPart, visibleCount, quotes)];
        if (visibleCount > 0) parts.Add(new(ReportInlineType.Text, ItemSeparator));
        parts.Add(new(
            ReportInlineType.Expander,
            FoldedListPrefix + (items.Count - visibleCount),
            BuildExpandedListParts(items, createPart)));
        return parts;
    }

    // Inline form: the items share one line, so a comma separates them. A name is quoted because it is
    // free text and may itself contain a comma, while an identifier is a digit run and needs no quote.
    private static List<ReportTextPart> BuildInlineListParts<T>(
        IReadOnlyList<T> items,
        Func<T, ReportTextPart> createPart,
        int count,
        ReportDisplayBudget? quotes)
    {
        List<ReportTextPart> parts = new(count == 0 ? 0 : count * 5 - 1);
        for (int index = 0; index < count; index++)
        {
            if (index > 0) parts.Add(new(ReportInlineType.Text, ItemSeparator));
            if (quotes is null)
            {
                parts.Add(createPart(items[index]));
                continue;
            }

            parts.Add(new(ReportInlineType.Text, quotes.ItemOpenQuote));
            parts.Add(createPart(items[index]));
            parts.Add(new(ReportInlineType.Text, quotes.ItemCloseQuote));
        }
        return parts;
    }

    // Expanded form: the revealed list lives in its own popup, so every item gets its own line and a
    // marker, which keeps even a name that contains the separator readable.
    private static List<ReportTextPart> BuildExpandedListParts<T>(
        IReadOnlyList<T> items,
        Func<T, ReportTextPart> createPart)
    {
        List<ReportTextPart> parts = new(items.Count == 0 ? 0 : items.Count * 3 - 1);
        for (int index = 0; index < items.Count; index++)
        {
            if (index > 0) parts.Add(new(ReportInlineType.LineBreak, LineBreakText));
            parts.Add(new(ReportInlineType.Text, ListItemMarker));
            parts.Add(createPart(items[index]));
        }
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
