using Revit.Linter.ElementDependencyDefiners.Abstractions;

namespace Revit.Linter.ElementDependencyDefiners.Infrastructure;

internal static class DefinerInstance<T>
    where T : IElementsDependencyDefiner, new()
{
    public static T Value { get; } = new();
}
