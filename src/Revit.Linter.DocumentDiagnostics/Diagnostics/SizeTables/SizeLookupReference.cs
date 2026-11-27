namespace Revit.Linter.DocumentDiagnostics.Diagnostics.SizeTables;

/// <summary>One <c>size_lookup</c> call with the values its arguments resolve to.</summary>
/// <param name="ParameterName">The parameter that owns the formula.</param>
/// <param name="TableNames">The distinct table names; empty when the argument has no static value.</param>
/// <param name="ColumnNames">The distinct column headers; empty when the argument has no static value.</param>
/// <param name="TableResolved">Whether the table argument is a literal or a family parameter with values.</param>
/// <param name="ColumnResolved">Whether the column argument is a literal or a family parameter with values.</param>
/// <param name="TableIsLiteral">Whether the table argument is a quoted value.</param>
/// <param name="ColumnIsLiteral">Whether the column argument is a quoted value.</param>
internal sealed record SizeLookupReference(
    string ParameterName,
    IReadOnlyList<string> TableNames,
    IReadOnlyList<string> ColumnNames,
    bool TableResolved,
    bool ColumnResolved,
    bool TableIsLiteral,
    bool ColumnIsLiteral);
