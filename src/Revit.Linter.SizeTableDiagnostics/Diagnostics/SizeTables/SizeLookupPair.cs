namespace Revit.Linter.SizeTableDiagnostics.Diagnostics.SizeTables;

/// <summary>One table and column pair read from a <c>size_lookup</c> call for a family type.</summary>
internal sealed record SizeLookupPair(string TableName, string ColumnName);
