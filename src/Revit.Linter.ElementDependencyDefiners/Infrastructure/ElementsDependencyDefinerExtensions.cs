using Revit.Linter.ElementDependencyDefiners.Abstractions;
using System.Reflection;

namespace Revit.Linter.ElementDependencyDefiners.Infrastructure;

/// <summary>
/// Discovers the built-in element dependency definer implementations.
/// </summary>
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

    /// <summary>
    /// Gets built-in dependency definer types that can be created without constructor arguments.
    /// </summary>
    /// <returns>A new mutable list of dependency definer types.</returns>
    public static IList<Type> GetWithEmptyConstructorElementsDependencyDefinerTypes()
        => [.. DefinerTypesWithEmptyConstructor];

    /// <summary>
    /// Gets all built-in dependency definer types, including composition and filtering definers.
    /// </summary>
    /// <returns>A new mutable list of dependency definer types.</returns>
    public static IList<Type> GetElementsDependencyDefinerTypes()
        => [.. DefinerTypes];
}
