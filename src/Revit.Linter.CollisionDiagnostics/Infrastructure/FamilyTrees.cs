using Revit.Linter.CollisionDiagnostics.Infrastructure.Extensions;

namespace Revit.Linter.CollisionDiagnostics.Infrastructure;

/// <summary>Answers whether two elements belong to one placed family.</summary>
/// <remarks>
/// Nested family instances form a tree through <c>FamilyInstance.SuperComponent</c>: a parent, its
/// nested components and everything below them all resolve to one top-level instance. Roots are
/// cached per element because a run meets the same elements as candidates for many targets. A
/// linked document is never walked: nesting cannot cross a Revit link.
/// </remarks>
internal sealed class FamilyTrees
{
    private readonly Dictionary<long, long> _rootIdByElementId = [];

    /// <summary>Gets whether both elements resolve to the same top-level family instance.</summary>
    internal bool AreRelated(Element first, Element second) => RootIdOf(first) == RootIdOf(second);

    private long RootIdOf(Element element)
    {
        long elementId = element.Id.IdValue();
        if (_rootIdByElementId.TryGetValue(elementId, out long knownRootId)) return knownRootId;

        Element root = element;
        while (root is FamilyInstance instance && instance.SuperComponent is { } parent)
            root = parent;

        long rootId = root.Id.IdValue();
        _rootIdByElementId[elementId] = rootId;
        return rootId;
    }
}
