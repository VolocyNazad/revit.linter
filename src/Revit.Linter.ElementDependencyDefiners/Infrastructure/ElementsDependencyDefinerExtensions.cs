using Revit.Linter.ElementDependencyDefiners.Abstractions;
using System.Reflection;

namespace Revit.Linter.ElementDependencyDefiners.Infrastructure;

public static class ElementsDependencyDefinerExtensions
{
    private static readonly HashSet<Type> TypesWithoutEmptyConstructor =
    [
        typeof(UnionDependencyDefiner),
        typeof(ElementFilterDependencyDefiner),
        typeof(ExceptDependencyDefiner),
        typeof(IntersectDependencyDefiner),
        typeof(WithElementFilterDependencyDefiner),
    ];

    private static readonly Type[] DefinerTypes =
    [..
        Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(type => typeof(IElementsDependencyDefiner).IsAssignableFrom(type))
            .Where(type => !type.IsInterface && !type.IsAbstract)
            .OrderBy(type => type.Name)
    ];

    private static readonly Type[] DefinerTypesWithEmptyConstructor =
        [.. DefinerTypes.Where(type => !TypesWithoutEmptyConstructor.Contains(type))];

    public static IList<Type> GetWithEmptyConstructorElementsDependencyDefinerTypes()
        => [.. DefinerTypesWithEmptyConstructor];

    public static IList<Type> GetElementsDependencyDefinerTypes()
        => [.. DefinerTypes];
}
