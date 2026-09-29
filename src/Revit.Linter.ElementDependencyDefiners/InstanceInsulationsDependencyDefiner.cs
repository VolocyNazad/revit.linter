using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Resolves insulation and lining elements attached to instances of the specified host type.
/// </summary>
public class InstanceInsulationsDependencyDefiner : IElementsDependencyDefiner
{
/// <inheritdoc />
public IElementsDependencyDefiner Inversed => DefinerInstance<MEPCurveHostTypeDependencyDefiner>.Value;

	/// <inheritdoc />
	public IEnumerable<Element> All(Element element)
	{
		if (element is not ElementType elementType) return [];

		IEnumerable<Element> instances = elementType.FindInstances();
		return instances.OfType<HostObject>()
			.SelectMany(host => host.FindInsulations());
	}

	/// <inheritdoc />
	public Element? FirstOrDefault(Element element) => All(element).FirstOrDefault();
}
