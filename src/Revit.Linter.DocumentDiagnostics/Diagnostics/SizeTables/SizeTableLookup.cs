namespace Revit.Linter.DocumentDiagnostics.Diagnostics.SizeTables;

/// <summary>Reads the stored size tables and their columns.</summary>
internal static class SizeTableLookup
{
    /// <summary>Finds the stored table that a reference names, including a name with an extension.</summary>
    /// <param name="manager">The size table manager, or <see langword="null"/>.</param>
    /// <param name="tableName">The name stored in a formula or a family type.</param>
    /// <returns>The stored table name, or <see langword="null"/> when the table is absent.</returns>
    public static string? FindTable(FamilySizeTableManager? manager, string tableName)
    {
        if (manager is null) return null;

        IReadOnlyList<string> storedNames = manager.GetAllSizeTableNames().ToArray();
        string? match = storedNames.FirstOrDefault(name =>
            string.Equals(name, tableName, StringComparison.OrdinalIgnoreCase));
        if (match is not null) return match;

        string withoutExtension = Path.GetFileNameWithoutExtension(tableName);
        return storedNames.FirstOrDefault(name =>
            string.Equals(name, withoutExtension, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Reads the column headers of a stored table.</summary>
    /// <param name="manager">The size table manager.</param>
    /// <param name="tableName">The stored table name.</param>
    /// <returns>The header names, or <see langword="null"/> when the table cannot be read.</returns>
    public static HashSet<string>? TryCollectColumnNames(
        FamilySizeTableManager manager, string tableName)
    {
        try
        {
            using FamilySizeTable table = manager.GetSizeTable(tableName);
            HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < table.NumberOfColumns; index++)
            {
                using FamilySizeTableColumn column = table.GetColumnHeader(index);
                names.Add(column.Name);
            }
            return names;
        }
        catch (Exception)
        {
            // A table that cannot be read is left out instead of failing the whole run.
            return null;
        }
    }
}
