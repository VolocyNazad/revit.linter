namespace Revit.Linter.SizeTableDiagnostics.Diagnostics.SizeTables;

/// <summary>Resolves the table and column arguments of one <c>size_lookup</c> call.</summary>
internal sealed class SizeLookupCallResolver(
    IReadOnlyDictionary<string, FamilyParameter> parameters,
    FamilyManager familyManager)
{
    /// <summary>Resolves one call for every family type.</summary>
    /// <param name="formulaParameterName">The parameter that owns the formula.</param>
    /// <param name="call">The parsed <c>size_lookup</c> call.</param>
    /// <returns>The table names, table and column pairs and the resolution flags.</returns>
    public SizeLookupReference Resolve(string formulaParameterName, SizeLookupCall call)
    {
        FamilyParameter? tableParameter = FindStringParameter(call.Table);
        FamilyParameter? columnParameter = FindStringParameter(call.Column);
        HashSet<string> tableNames = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> columnNames = new(StringComparer.OrdinalIgnoreCase);
        List<SizeLookupPair> pairs = [];
        HashSet<string> seenPairs = new(StringComparer.OrdinalIgnoreCase);
        bool tableAllTypesRead = true;
        bool columnAllTypesRead = true;

        if (call.Table.Literal is not null && call.Column.Literal is not null)
        {
            tableNames.Add(call.Table.Literal);
            columnNames.Add(call.Column.Literal);
            pairs.Add(new SizeLookupPair(call.Table.Literal, call.Column.Literal));
        }
        else
        {
            foreach (FamilyType type in familyManager.Types)
            {
                (string? tableName, bool tableRead) = ReadValue(call.Table, tableParameter, type);
                (string? columnName, bool columnRead) = ReadValue(call.Column, columnParameter, type);
                if (!tableRead) tableAllTypesRead = false;
                if (!columnRead) columnAllTypesRead = false;
                if (!string.IsNullOrWhiteSpace(tableName)) tableNames.Add(tableName.Trim());
                if (!string.IsNullOrWhiteSpace(columnName)) columnNames.Add(columnName.Trim());
                if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(columnName)) continue;

                string tableValue = tableName.Trim();
                string columnValue = columnName.Trim();
                if (seenPairs.Add($"{tableValue}\u0000{columnValue}"))
                    pairs.Add(new SizeLookupPair(tableValue, columnValue));
            }
        }

        bool tableResolved = call.Table.Literal is not null
            || (tableAllTypesRead && tableNames.Count > 0);
        bool columnResolved = call.Column.Literal is not null
            || (columnAllTypesRead && columnNames.Count > 0);
        return new SizeLookupReference(
            formulaParameterName,
            tableNames.ToArray(),
            pairs,
            tableResolved,
            columnResolved);
    }

    private FamilyParameter? FindStringParameter(SizeLookupArgument argument)
    {
        if (argument.Literal is not null || argument.ParameterName is null) return null;
        return parameters.TryGetValue(argument.ParameterName, out FamilyParameter? parameter)
            && parameter.StorageType == StorageType.String
            ? parameter
            : null;
    }

    private static (string? Value, bool Read) ReadValue(
        SizeLookupArgument argument,
        FamilyParameter? parameter,
        FamilyType type)
    {
        if (argument.Literal is not null) return (argument.Literal, true);
        if (parameter is null) return (null, false);
        try
        {
            return (type.AsString(parameter), true);
        }
        catch (Exception)
        {
            // A value that cannot be read for one type makes the argument unresolved: that type
            // may use a table or column the other types do not mention.
            return (null, false);
        }
    }
}
