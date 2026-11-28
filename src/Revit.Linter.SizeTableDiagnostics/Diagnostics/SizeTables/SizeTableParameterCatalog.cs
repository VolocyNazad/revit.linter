namespace Revit.Linter.SizeTableDiagnostics.Diagnostics.SizeTables;

/// <summary>Reads the family parameters a size lookup formula can reference.</summary>
internal static class SizeTableParameterCatalog
{
    /// <summary>Reads the family parameters and the built-in lookup table name.</summary>
    /// <param name="familyManager">The manager of the family document.</param>
    /// <returns>The parameters keyed by the name a formula uses.</returns>
    public static IReadOnlyDictionary<string, FamilyParameter> Read(FamilyManager familyManager)
    {
        Dictionary<string, FamilyParameter> parameters = familyManager.GetParameters()
            .GroupBy(parameter => parameter.Definition.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        // The built-in lookup table name is not part of the family parameter set, but a formula can
        // use it as the table argument. Add it under the name the formula editor shows.
        FamilyParameter? lookupTableName;
        try
        {
            lookupTableName = familyManager.get_Parameter(BuiltInParameter.RBS_LOOKUP_TABLE_NAME);
        }
        catch (Exception)
        {
            // A family shape that cannot read the parameter has no table name to resolve.
            lookupTableName = null;
        }

        // Dictionary<TKey, TValue>.TryAdd is not available on net48; keep the same first-wins rule.
        if (lookupTableName is not null
            && !parameters.ContainsKey(lookupTableName.Definition.Name))
        {
            parameters.Add(lookupTableName.Definition.Name, lookupTableName);
        }

        return parameters;
    }
}
