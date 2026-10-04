namespace Revit.Linter.ProjectParameterManaging.Infrastructure.Extensions;

internal static class CollectionExtensions
{
    // Equal sets of different lengths are not equal: [1, 2] differs from [1, 2, 2]. Each sequence is
    // enumerated once, because the callers pass lazy queries over Revit API collections.
    internal static bool SetEquals<T>(this IEnumerable<T> first, IEnumerable<T> second)
    {
        List<T> firstItems = first.ToList();
        List<T> secondItems = second.ToList();
        return firstItems.Count == secondItems.Count && new HashSet<T>(firstItems).SetEquals(secondItems);
    }
}
