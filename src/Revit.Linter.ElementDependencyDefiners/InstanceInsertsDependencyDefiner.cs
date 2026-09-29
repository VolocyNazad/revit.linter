using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Resolves elements inserted into instances of the specified host type.
/// </summary>
public class InstanceInsertsDependencyDefiner : IElementsDependencyDefiner
{
/// <inheritdoc />
public IElementsDependencyDefiner Inversed => DefinerInstance<HostTypeDependencyDefiner>.Value;

	/// <inheritdoc />
	public IEnumerable<Element> All(Element element)
	{
		if (element is not ElementType elementType) return [];

		IEnumerable<Element> instances = elementType.FindInstances();

		return instances
			.SelectMany(i => i switch {
				HostObject hostObject => hostObject.FindInserted(),
				FamilyInstance familyInstance => familyInstance.FindInserted(),
				_ => []
			});
	}

	/// <inheritdoc />
	public Element? FirstOrDefault(Element element) => All(element).FirstOrDefault();
}
