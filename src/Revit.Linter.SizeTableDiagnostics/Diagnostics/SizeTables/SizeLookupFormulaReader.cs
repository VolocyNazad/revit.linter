namespace Revit.Linter.SizeTableDiagnostics.Diagnostics.SizeTables;

/// <summary>Reads every <c>size_lookup</c> call from the family formulas.</summary>
internal static class SizeLookupFormulaReader
{
    /// <summary>Reads the formulas and resolves their table references.</summary>
    /// <param name="parameters">The family parameters keyed by the name a formula uses.</param>
    /// <param name="resolver">The resolver for one call.</param>
    /// <returns>The references and the aggregate flags.</returns>
    public static SizeLookupFormulaReading Read(
        IReadOnlyDictionary<string, FamilyParameter> parameters,
        SizeLookupCallResolver resolver)
    {
        List<SizeLookupReference> references = [];
        HashSet<string> usedTableNames = new(StringComparer.OrdinalIgnoreCase);
        bool hasUnresolvedTableReferences = false;
        bool allTableNamesAreLiteral = true;

        foreach (FamilyParameter parameter in parameters.Values)
        {
            string? formula;
            string parameterName;
            try
            {
                formula = parameter.Formula;
                parameterName = parameter.Definition.Name;
            }
            catch (Exception)
            {
                // A parameter whose formula cannot be read has no reference this run.
                continue;
            }

            if (string.IsNullOrWhiteSpace(formula)) continue;

            foreach (SizeLookupCall call in SizeLookupFormulaParser.Parse(formula, parameterName))
            {
                SizeLookupReference reference = resolver.Resolve(parameterName, call);
                references.Add(reference);
                if (!reference.TableResolved) hasUnresolvedTableReferences = true;
                if (call.Table.Literal is null) allTableNamesAreLiteral = false;
                AddUsedTableNames(usedTableNames, reference.TableNames);
            }
        }

        return new SizeLookupFormulaReading(
            references,
            usedTableNames,
            hasUnresolvedTableReferences,
            allTableNamesAreLiteral);
    }

    private static void AddUsedTableNames(HashSet<string> usedTableNames, IReadOnlyList<string> tableNames)
    {
        foreach (string tableName in tableNames)
        {
            usedTableNames.Add(tableName);
            usedTableNames.Add(Path.GetFileName(tableName));
            usedTableNames.Add(Path.GetFileNameWithoutExtension(tableName));
        }
    }
}
