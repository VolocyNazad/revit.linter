namespace Revit.Linter.TypeCatalogDiagnostics.Diagnostics.FamilyTypeCatalog;

/// <summary>Names one parameter column of a type catalog.</summary>
/// <param name="Header">The header field written in the catalog, for example <c>Height##LENGTH##MILLIMETERS</c>.</param>
/// <param name="ParameterName">The parameter name before the first <c>##</c>.</param>
/// <param name="Unit">The unit the column values are written in, or <see langword="null"/> for <c>OTHER</c>.</param>
internal sealed record FamilyTypeCatalogColumn(string Header, string ParameterName, ForgeTypeId? Unit)
{
    /// <summary>Reads a column from an existing catalog header field.</summary>
    public static FamilyTypeCatalogColumn FromHeader(string header)
    {
        int suffix = header.IndexOf("##", StringComparison.Ordinal);
        string name = (suffix < 0 ? header : header.Substring(0, suffix)).Trim();
        ForgeTypeId? unit = null;
        if (suffix >= 0)
        {
            string[] parts = header.Substring(suffix + 2).Split(new[] { "##" }, StringSplitOptions.None);
            if (parts.Length >= 2) unit = FamilyTypeCatalogUnits.FindUnit(parts[1].Trim());
        }
        return new(header, name, unit);
    }

    /// <summary>Builds a column from a family parameter.</summary>
    public static FamilyTypeCatalogColumn FromParameter(FamilyParameter parameter)
    {
        string name = parameter.Definition.Name;
        (string spec, string unit, ForgeTypeId? unitId) = FamilyTypeCatalogUnits.Read(parameter);
        string header = unit.Length == 0 ? $"{name}##{spec}##" : $"{name}##{spec}##{unit}";
        return new(header, name, unitId);
    }
}
