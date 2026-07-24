using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Позволяет определить экземпляры изоляции, для текущего экземпляра
/// </summary>
public class InsulationsDependencyDefiner : IElementsDependencyDefiner
{
public IElementsDependencyDefiner Inversed => DefinerInstance<MEPCurveHostDependencyDefiner>.Value;

	public IEnumerable<Element> All(Element element) =>
		element is { } hostObject
			? hostObject.FindInsulations()
			: [];

	public Element? FirstOrDefault(Element element) =>
		element.FindInsulations().FirstOrDefault();
}
