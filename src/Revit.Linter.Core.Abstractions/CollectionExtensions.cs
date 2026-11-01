namespace Revit.Linter.Core.Abstractions;

/// <summary>
/// Collection comparison helpers shared by diagnostic and managing modules.
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Checks that two sequences contain the same items regardless of order.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="first">The first sequence to compare.</param>
    /// <param name="second">The second sequence to compare.</param>
    /// <returns>True when both sequences contain equal items; otherwise false.</returns>
    /// <remarks>
    /// Sequences with equal distinct items but different lengths are not equal:
    /// <c>[1, 2]</c> differs from <c>[1, 2, 2]</c>. Each sequence is enumerated once,
    /// because the callers pass lazy queries over Revit API collections.
    /// </remarks>
    public static bool SetEquals<T>(this IEnumerable<T> first, IEnumerable<T> second)
    {
        List<T> firstItems = first.ToList();
        List<T> secondItems = second.ToList();
        return firstItems.Count == secondItems.Count && new HashSet<T>(firstItems).SetEquals(secondItems);
    }
}
