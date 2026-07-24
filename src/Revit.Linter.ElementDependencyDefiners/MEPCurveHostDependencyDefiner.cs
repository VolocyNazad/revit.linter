using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Позволяет определить экземпляр, который является основой для текущей изоляции
/// </summary>
public class MEPCurveHostDependencyDefiner : IElementsDependencyDefiner
{
public IElementsDependencyDefiner Inversed => DefinerInstance<InsulationsDependencyDefiner>.Value;

	public IEnumerable<Element> All(Element element)
	{
		var collection = new List<Element>();

		if (element is not InsulationLiningBase insulation) return collection;

		ElementId hostId = insulation.HostElementId;
		if (hostId is null) return collection;

		Element? host = element.GetElementHere(hostId);
		if (host is null) return collection;

		collection.Add(host);
		return collection;
	}

	public Element? FirstOrDefault(Element element)
	{
		if (element is not InsulationLiningBase insulation) return null;
		ElementId hostId = insulation.HostElementId;
		if (hostId is null) return null;

		return element.GetElementHere(hostId);
	}
}
