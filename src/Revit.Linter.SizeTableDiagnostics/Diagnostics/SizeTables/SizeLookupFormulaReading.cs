namespace Revit.Linter.SizeTableDiagnostics.Diagnostics.SizeTables;

/// <summary>The table references read from every family formula.</summary>
/// <param name="References">The resolved <c>size_lookup</c> calls.</param>
/// <param name="UsedTableNames">Every table name a formula refers to, including file-name forms.</param>
/// <param name="HasUnresolvedTableReferences">Whether a table name cannot be determined.</param>
/// <param name="AllTableNamesAreLiteral">Whether every table argument is a quoted value.</param>
internal sealed record SizeLookupFormulaReading(
    IReadOnlyList<SizeLookupReference> References,
    HashSet<string> UsedTableNames,
    bool HasUnresolvedTableReferences,
    bool AllTableNamesAreLiteral);
