namespace Revit.Linter.SizeTableDiagnostics.Diagnostics.SizeTables;

/// <summary>One <c>size_lookup</c> call with the values its arguments resolve to.</summary>
/// <param name="ParameterName">The parameter that owns the formula.</param>
/// <param name="TableNames">The distinct table names; empty when the argument has no static value.</param>
/// <param name="Pairs">The table and column pairs read for each family type.</param>
/// <param name="TableResolved">Whether the table argument is a literal or a family parameter with values.</param>
/// <param name="ColumnResolved">Whether the column argument is a literal or a family parameter with values.</param>
internal sealed record SizeLookupReference(
    string ParameterName,
    IReadOnlyList<string> TableNames,
    IReadOnlyList<SizeLookupPair> Pairs,
    bool TableResolved,
    bool ColumnResolved);
