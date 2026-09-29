namespace Revit.Linter.ProjectParameterManaging.Infrastructure.Extensions;

internal static class CollectionExtensions
{
    internal static bool SetEquals<T>(this IEnumerable<T> first, IEnumerable<T> second)
    {
        return first.Count() == second.Count() &&
               !first.Except(second).Any() &&
               !second.Except(first).Any();
    }
}
