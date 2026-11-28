namespace Revit.Linter.SizeTableDiagnostics.Diagnostics.SizeTables;

/// <summary>The size lookup tables of a family and every name that refers to one.</summary>
/// <param name="Manager">The size table manager, or <see langword="null"/> when the family has none.</param>
/// <param name="TableNames">The table names stored in the family.</param>
/// <param name="UsedTableNames">Every table name found in formulas.</param>
/// <param name="LookupReferences">The parsed <c>size_lookup</c> calls.</param>
/// <param name="HasUnresolvedTableReferences">Whether a table argument cannot be resolved statically.</param>
/// <param name="AllTableNamesAreLiteral">Whether every table argument is a quoted value.</param>
internal sealed record SizeTableReferenceCollection(
    FamilySizeTableManager? Manager,
    IReadOnlyList<string> TableNames,
    HashSet<string> UsedTableNames,
    IReadOnlyList<SizeLookupReference> LookupReferences,
    bool HasUnresolvedTableReferences,
    bool AllTableNamesAreLiteral)
{
    /// <summary>Gets a collection for a document that has no size tables or no family.</summary>
    public static SizeTableReferenceCollection Empty { get; } = new(
        null, [], new HashSet<string>(StringComparer.OrdinalIgnoreCase), [], false, true);

    /// <summary>Gets a value indicating whether a stored table is referenced by a type or a formula.</summary>
    /// <param name="tableName">The stored table name.</param>
    /// <returns><see langword="true"/> when a type or a formula refers to the table.</returns>
    public bool IsReferenced(string tableName) =>
        UsedTableNames.Contains(tableName)
        || UsedTableNames.Contains(Path.GetFileNameWithoutExtension(tableName));
}
