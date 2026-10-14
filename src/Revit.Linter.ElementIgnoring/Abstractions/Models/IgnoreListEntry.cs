namespace Revit.Linter.ElementIgnoring.Abstractions.Models;

/// <summary>
/// Describes one entry of the ignore list of a document.
/// </summary>
/// <param name="ElementIdValue">The numeric value of the ignored element identifier, or <see langword="null"/> for a document-level entry.</param>
/// <param name="Code">The diagnostic code the element or document is ignored for.</param>
/// <param name="UniqueId">The unique identifier of the element when it was ignored; empty for a document-level entry.</param>
/// <param name="Exists">Whether the ignored element or document still exists.</param>
public sealed record IgnoreListEntry(long? ElementIdValue, string Code, string UniqueId, bool Exists);
