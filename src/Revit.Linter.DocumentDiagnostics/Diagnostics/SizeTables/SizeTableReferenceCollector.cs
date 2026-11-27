using Revit.Linter.DocumentQueries.Abstractions.Services;

namespace Revit.Linter.DocumentDiagnostics.Diagnostics.SizeTables;

/// <summary>Collects every size lookup table reference of a family document.</summary>
internal sealed class SizeTableReferenceCollector(IDocumentQueryService documentQueries)
{
    /// <summary>Reads the types and formulas of a family and resolves their table references.</summary>
    /// <param name="targetDocument">The document to inspect.</param>
    /// <returns>The stored tables and every name that refers to one.</returns>
    public SizeTableReferenceCollection Collect(Document targetDocument)
    {
        Family? family = targetDocument.OwnerFamily;
        if (family is null) return SizeTableReferenceCollection.Empty;

        FamilyManager familyManager = targetDocument.FamilyManager;
        FamilySizeTableManager? manager =
            FamilySizeTableManager.GetFamilySizeTableManager(targetDocument, family.Id);
        IReadOnlyDictionary<string, FamilyParameter> parameters = CollectParameters(familyManager);

        List<SizeLookupReference> lookupReferences = [];
        HashSet<string> usedTableNames = new(StringComparer.OrdinalIgnoreCase);
        bool hasUnresolvedTableReferences = false;
        foreach (FamilyParameter parameter in parameters.Values)
        {
            string? formula = parameter.Formula;
            if (string.IsNullOrWhiteSpace(formula)) continue;

            foreach (SizeLookupCall call in SizeLookupFormulaParser.Parse(formula, parameter.Definition.Name))
            {
                IReadOnlyList<string> tableNames = ResolveValues(
                    call.Table, parameters, familyManager, out bool tableResolved);
                IReadOnlyList<string> columnNames = ResolveValues(
                    call.Column, parameters, familyManager, out bool columnResolved);
                if (!tableResolved) hasUnresolvedTableReferences = true;
                foreach (string tableName in tableNames) usedTableNames.Add(tableName);
                lookupReferences.Add(new SizeLookupReference(
                    parameter.Definition.Name,
                    tableNames,
                    columnNames,
                    tableResolved,
                    columnResolved,
                    call.Table.Literal is not null,
                    call.Column.Literal is not null));
            }
        }

        List<SizeTableTypeReference> typeReferences =
            CollectTypeReferences(targetDocument, usedTableNames);
        IReadOnlyList<string> storedTableNames = manager is null
            ? []
            : manager.GetAllSizeTableNames().ToArray();

        return new SizeTableReferenceCollection(
            manager,
            storedTableNames,
            usedTableNames,
            lookupReferences,
            typeReferences,
            hasUnresolvedTableReferences);
    }

    private static IReadOnlyDictionary<string, FamilyParameter> CollectParameters(FamilyManager familyManager) =>
        familyManager.GetParameters()
            .GroupBy(parameter => parameter.Definition.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

    private static IReadOnlyList<string> ResolveValues(
        SizeLookupArgument argument,
        IReadOnlyDictionary<string, FamilyParameter> parameters,
        FamilyManager familyManager,
        out bool resolved)
    {
        resolved = false;
        if (argument.Literal is not null)
        {
            resolved = true;
            return [argument.Literal];
        }
        if (argument.ParameterName is null) return [];
        if (!parameters.TryGetValue(argument.ParameterName, out FamilyParameter? parameter)) return [];
        if (parameter.StorageType != StorageType.String) return [];

        HashSet<string> values = new(StringComparer.OrdinalIgnoreCase);
        foreach (FamilyType type in familyManager.Types)
        {
            try
            {
                string? value = type.AsString(parameter);
                if (!string.IsNullOrWhiteSpace(value)) values.Add(value.Trim());
            }
            catch (Exception)
            {
                // An instance parameter or a value that cannot be read for this type has no
                // single static value; the call stays unresolved instead of producing a guess.
            }
        }
        resolved = values.Count > 0;
        return values.ToArray();
    }

    private List<SizeTableTypeReference> CollectTypeReferences(
        Document targetDocument, HashSet<string> usedTableNames)
    {
        List<SizeTableTypeReference> references = [];
        // Family symbols are shared with the element diagnostics of the same run through the
        // class query instead of a dedicated collector.
        IReadOnlyList<FamilySymbol> symbols = documentQueries.GetElementsOfClass<FamilySymbol>(targetDocument);
        foreach (FamilySymbol symbol in symbols)
        {
            string? rawName = symbol
                .get_Parameter(BuiltInParameter.RBS_LOOKUP_TABLE_NAME)
                ?.AsString();
            // Explicit null check: string.IsNullOrWhiteSpace carries no NotNullWhen contract
            // on net48, so it alone does not narrow the nullable on every target framework.
            if (rawName is null || string.IsNullOrWhiteSpace(rawName)) continue;

            string tableName = rawName.Trim();
            references.Add(new SizeTableTypeReference(symbol.Name, tableName));
            usedTableNames.Add(tableName);
            usedTableNames.Add(Path.GetFileName(tableName));
            usedTableNames.Add(Path.GetFileNameWithoutExtension(tableName));
        }
        return references;
    }
}
