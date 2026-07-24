using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Позволяет определить экземпляр внешней изоляции, для текущего экземпляра
/// </summary>
public class InternalInsulationDependencyDefiner : IElementsDependencyDefiner
{
public IElementsDependencyDefiner Inversed => DefinerInstance<MEPCurveHostDependencyDefiner>.Value;

	public IEnumerable<Element> All(Element element) =>
		element is { } hostObject
			? hostObject.FindInternalInsulations()
			: [];

	public Element? FirstOrDefault(Element element) =>
		element is { } hostObject
			? hostObject.FindInternalInsulations().FirstOrDefault()
			: null;
}
