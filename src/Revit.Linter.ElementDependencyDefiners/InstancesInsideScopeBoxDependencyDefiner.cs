using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Resolves elements that intersect a scope box.
/// </summary>
public class InstancesInsideScopeBoxDependencyDefiner : IElementsDependencyDefiner
{
/// <inheritdoc />
public IElementsDependencyDefiner Inversed => DefinerInstance<ScopeBoxDependencyDefiner>.Value;

	/// <inheritdoc />
	public IEnumerable<Element> All(Element element) => element.Category?.GetBuiltInCategory() == BuiltInCategory.OST_VolumeOfInterest
			? element.FindPlacedInsideScopeBox()
			: [];

	/// <inheritdoc />
	public Element? FirstOrDefault(Element element) => element.Category?.GetBuiltInCategory() == BuiltInCategory.OST_VolumeOfInterest
			? element.FindPlacedInsideScopeBox().FirstOrDefault()
			: null;
}
