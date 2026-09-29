using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;
using System.Diagnostics.CodeAnalysis;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Resolves the type of the MEP curve that hosts an insulation or lining element.
/// </summary>
[SuppressMessage("SonarAnalyzer", "S101", Justification = "MEP is a standard abbreviation")]
public class MEPCurveHostTypeDependencyDefiner : IElementsDependencyDefiner
{
/// <inheritdoc />
public IElementsDependencyDefiner Inversed => DefinerInstance<InstanceInsulationsDependencyDefiner>.Value;

	/// <inheritdoc />
	public IEnumerable<Element> All(Element element)
	{
		var collection = new List<Element>();

		if (element is not InsulationLiningBase insulation) return collection;

		ElementId hostId = insulation.HostElementId;
		if (hostId is null) return collection;

		Element? host = element.GetElementHere(hostId);
		if (host is null) return collection;

		ElementType? type = host.FindElementType();
		if (type is null) return collection;

		collection.Add(type);
		return collection;
	}

	/// <inheritdoc />
	public Element? FirstOrDefault(Element element)
	{
		if (element is not InsulationLiningBase insulation) return null;
		ElementId hostId = insulation.HostElementId;
		if (hostId is null) return null;

		Element? host = element.GetElementHere(hostId);

		return host?.FindElementType();
	}
}
