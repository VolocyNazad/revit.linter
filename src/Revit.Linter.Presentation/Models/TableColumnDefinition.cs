namespace Revit.Linter.Presentation.Models;

/// <summary>
/// Describes one hideable column of a table for its visibility popup.
/// </summary>
/// <param name="Key">The stored column key, one of <see cref="TableColumnKeys"/>.</param>
/// <param name="Title">Resolves the localized column title, so it follows the current user interface culture.</param>
public sealed record TableColumnDefinition(string Key, Func<string> Title);
