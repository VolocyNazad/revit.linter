namespace Revit.Linter.DocumentDiagnostics.Diagnostics.SizeTables;

/// <summary>One <c>size_lookup</c> call found in a family formula.</summary>
/// <param name="FormulaParameterName">The parameter that owns the formula.</param>
/// <param name="Table">The table name argument.</param>
/// <param name="Column">The column header argument.</param>
internal sealed record SizeLookupCall(
    string FormulaParameterName,
    SizeLookupArgument Table,
    SizeLookupArgument Column);
