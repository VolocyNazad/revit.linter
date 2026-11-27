namespace Revit.Linter.DocumentDiagnostics.Diagnostics.SizeTables;

/// <summary>One family type that names a size lookup table through its built-in parameter.</summary>
/// <param name="TypeName">The family type name.</param>
/// <param name="TableName">The table name stored in the type.</param>
internal sealed record SizeTableTypeReference(string TypeName, string TableName);
