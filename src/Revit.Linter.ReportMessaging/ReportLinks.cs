namespace Revit.Linter.ReportMessaging;

/// <summary>
/// Creates report links from scalar and sequence values.
/// </summary>
public static class ReportLinks
{
    /// <summary>
    /// Converts a compatible scalar or sequence value into report links.
    /// </summary>
    /// <typeparam name="T">The supported value type.</typeparam>
    /// <param name="value">The scalar or sequence value to convert.</param>
    /// <param name="textSelector">A function that creates display text for each value.</param>
    /// <returns>
    /// The created links, or <see langword="null"/> when <paramref name="value"/> is not a
    /// <typeparamref name="T"/> or an <see cref="IEnumerable{T}"/> of compatible values.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="textSelector"/> is <see langword="null"/>.
    /// </exception>
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
