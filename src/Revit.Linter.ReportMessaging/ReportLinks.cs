namespace Revit.Linter.ReportMessaging;

public static class ReportLinks
{
    public static IReadOnlyList<ReportLink>? TryGetLinks<T>(object? value, Func<T, string> textSelector)
    {
        if (textSelector is null)
            throw new ArgumentNullException(nameof(textSelector));

        return value switch
        {
            IEnumerable<T> items => items.Select(item => new ReportLink(textSelector(item), item)).ToList(),
            T item => [new(textSelector(item), item)],
            _ => null,
        };
    }
}
