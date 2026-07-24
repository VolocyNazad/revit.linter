using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Позволяет определить экземпляры, которые пересекаются со случайной областью видимости
/// </summary>
public class InstancesInsideScopeBoxDependencyDefiner : IElementsDependencyDefiner
{
public IElementsDependencyDefiner Inversed => DefinerInstance<ScopeBoxDependencyDefiner>.Value;

	public IEnumerable<Element> All(Element element) => element.Category?.GetBuiltInCategory() == BuiltInCategory.OST_VolumeOfInterest
			? element.FindPlacedInsideScopeBox()
			: [];

	public Element? FirstOrDefault(Element element) => element.Category?.GetBuiltInCategory() == BuiltInCategory.OST_VolumeOfInterest
			? element.FindPlacedInsideScopeBox().FirstOrDefault()
			: null;
}
