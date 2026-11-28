namespace Revit.Linter.SizeTableDiagnostics.Diagnostics.SizeTables;

/// <summary>Collects every size lookup table reference of a family document.</summary>
internal static class SizeTableReferenceCollector
{
    /// <summary>Reads the family and collects its table references.</summary>
    /// <param name="targetDocument">The document to inspect.</param>
    /// <returns>The stored tables and every table name a formula refers to.</returns>
    public static SizeTableReferenceCollection Collect(Document targetDocument)
    {
        Family? family = targetDocument.OwnerFamily;
        if (family is null) return SizeTableReferenceCollection.Empty;

        FamilyManager familyManager = targetDocument.FamilyManager;
        FamilySizeTableManager? manager =
            FamilySizeTableManager.GetFamilySizeTableManager(targetDocument, family.Id);
        IReadOnlyDictionary<string, FamilyParameter> parameters =
            SizeTableParameterCatalog.Read(familyManager);
        SizeLookupFormulaReading reading = SizeLookupFormulaReader.Read(
            parameters, new SizeLookupCallResolver(parameters, familyManager));

        IReadOnlyList<string> storedTableNames = manager is null
            ? []
            : manager.GetAllSizeTableNames().ToArray();

        return new SizeTableReferenceCollection(
            manager,
            storedTableNames,
            reading.UsedTableNames,
            reading.References,
            reading.HasUnresolvedTableReferences,
            reading.AllTableNamesAreLiteral);
    }
}
