using Autodesk.Revit.DB;

namespace Revit.Linter.ElementDependencyDefiners.Abstractions;

/// <summary>
/// Resolves elements related to a source element by a specific dependency relationship.
/// </summary>
public interface IElementsDependencyDefiner
{
	/// <summary>
	/// Gets a definer that resolves the inverse relationship, when one is available.
	/// </summary>
	IElementsDependencyDefiner? Inversed { get; }

	/// <summary>
	/// Resolves all elements related to the specified source element.
	/// </summary>
	/// <param name="element">The source element.</param>
	/// <returns>The related elements.</returns>
	IEnumerable<Element> All(Element element);

	/// <summary>
	/// Resolves the first element related to the specified source element.
	/// </summary>
	/// <param name="element">The source element.</param>
	/// <returns>The first related element, or <see langword="null"/> when no related element exists.</returns>
	Element? FirstOrDefault(Element element);
}
