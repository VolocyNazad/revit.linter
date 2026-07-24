using Autodesk.Revit.DB;

namespace Revit.Linter.ElementDependencyDefiners.Abstractions;

public interface IElementsDependencyDefiner
{
	IElementsDependencyDefiner? Inversed { get; }
	IEnumerable<Element> All(Element element);
	Element? FirstOrDefault(Element element);
}