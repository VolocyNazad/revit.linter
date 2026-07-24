using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Позволяет определить экземпляры, для которых изоляция является типоразмером основы
/// </summary>
public class InstanceInsulationsDependencyDefiner : IElementsDependencyDefiner
{
public IElementsDependencyDefiner Inversed => DefinerInstance<MEPCurveHostTypeDependencyDefiner>.Value;

	public IEnumerable<Element> All(Element element)
	{
		if (element is not ElementType elementType) return [];

		IEnumerable<Element> instances = elementType.FindInstances();
		return instances.OfType<HostObject>()
			.SelectMany(host => host.FindInsulations());
	}

	public Element? FirstOrDefault(Element element) => All(element).FirstOrDefault();
}
