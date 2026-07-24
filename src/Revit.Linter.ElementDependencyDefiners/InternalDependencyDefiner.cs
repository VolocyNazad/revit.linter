using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Определяет самого себя
/// </summary>
public class InternalDependencyDefiner : IElementsDependencyDefiner
{
public IElementsDependencyDefiner Inversed => DefinerInstance<InternalDependencyDefiner>.Value;

	public IEnumerable<Element> All(Element element) 
		=> [element];

	public Element FirstOrDefault(Element element) => element;
}
