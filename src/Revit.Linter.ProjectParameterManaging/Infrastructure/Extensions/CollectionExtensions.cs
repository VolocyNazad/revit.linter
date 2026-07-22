namespace Revit.Linter.ProjectParameterManaging.Infrastructure.Extensions;

public static class CollectionExtensions
{
    public static bool SetEquals<T>(this IEnumerable<T> first, IEnumerable<T> second)
    {
        return first.Count() == second.Count() &&
               !first.Except(second).Any() &&
               !second.Except(first).Any();
    }
}