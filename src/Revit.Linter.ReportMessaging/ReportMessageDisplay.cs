namespace Revit.Linter.ReportMessaging;

/// <summary>
/// Holds the display and the complete text of one report message.
/// </summary>
/// <remarks>
/// A report table renders <see cref="Parts"/>, where a list value is folded into its first items and an
/// expander that carries the complete list. <see cref="Text"/> keeps the complete message for the
/// search, the sort and the export, so folding never loses data. Both forms are parsed on first use,
/// because most rows are never unfolded.
/// </remarks>
public sealed class ReportMessageDisplay
{
    private readonly string _template;
    private readonly IReadOnlyDictionary<string, object> _args;
    private readonly Func<object?, IReadOnlyList<ReportLink>?>? _linkProvider;
    private readonly ReportDisplayBudget _budget;

    private ReportMessage? _message;
    private IReadOnlyList<ReportTextPart>? _parts;

    /// <summary>
    /// Initializes a new display of a report message.
    /// </summary>
    /// <param name="template">The message template.</param>
    /// <param name="args">The values keyed by placeholder name.</param>
    /// <param name="linkProvider">
    /// An optional function that converts a placeholder value into interactive links.
    /// </param>
    /// <param name="budget">The limit applied to every list value.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="template"/>, <paramref name="args"/> or <paramref name="budget"/> is
    /// <see langword="null"/>.
    /// </exception>
    public ReportMessageDisplay(
        string template,
        IReadOnlyDictionary<string, object> args,
        Func<object?, IReadOnlyList<ReportLink>?>? linkProvider,
        ReportDisplayBudget budget)
    {
        _template = template ?? throw new ArgumentNullException(nameof(template));
        _args = args ?? throw new ArgumentNullException(nameof(args));
        _linkProvider = linkProvider;
        _budget = budget ?? throw new ArgumentNullException(nameof(budget));
    }

    /// <summary>Gets the complete message with its rich parts.</summary>
    public ReportMessage FullMessage => _message ??= ReportMessageParser.Parse(
        _template,
        _args,
        _linkProvider);

    /// <summary>Gets the display parts, where a list that exceeds the budget is folded.</summary>
    public IReadOnlyList<ReportTextPart> Parts => _parts ??= ReportMessageParser.ParseTemplateForDisplay(
        _template,
        _args,
        _linkProvider,
        _budget);

    /// <summary>Gets the complete message without rich formatting.</summary>
    public string Text => FullMessage.Text;
}
