namespace Revit.Linter.TypeCatalogDiagnostics;

internal static class TypeCatalogDiagnosticIdCollector
{
    public static readonly ElementDiagnosticId ManyFamilyTypes = Create("SHRD0046", DiagnosticSeverity.Message);
    public static readonly ElementDiagnosticId FamilyTypesMissingFromCatalog = Create("SHRD0047", DiagnosticSeverity.Warning);
    public static readonly ElementDiagnosticId FamilyTypeCatalogInUse = Create("SHRD0048", DiagnosticSeverity.Message);
    public static readonly ElementDiagnosticId FamilyTypeCatalogMissingParameters = Create("SHRD0049", DiagnosticSeverity.Warning);

    private static ElementDiagnosticId Create(string code, DiagnosticSeverity severity)
    {
        List<string> tags = [DiagnosticTags.BuiltIn];
        if (DiagnosticTags.DisciplineForCode(code) is { } discipline) tags.Add(discipline);
        tags.Add(DiagnosticTags.Family);

        return new(
            code,
            TypeCatalogDiagnosticLocalizations.GetString($"{code}_description"),
            TypeCatalogDiagnosticLocalizations.GetString($"{code}_message"),
            severity,
            true,
            false,
            string.Empty,
            systemTags: tags);
    }
}
