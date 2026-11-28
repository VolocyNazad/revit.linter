using System.Globalization;

namespace Revit.Linter.TypeCatalogDiagnostics.Diagnostics.FamilyTypeCatalog;

/// <summary>Applies the values of one catalog row to a family type.</summary>
internal static class FamilyTypeCatalogValues
{
    /// <summary>Sets the values of one catalog row on the type.</summary>
    /// <param name="document">The family document.</param>
    /// <param name="type">The type the row describes.</param>
    /// <param name="columns">The catalog columns.</param>
    /// <param name="fields">The fields of the row, where the first field is the type name.</param>
    /// <returns><see langword="true"/> when every value was applied.</returns>
    public static bool Apply(
        Document document,
        FamilyType type,
        IReadOnlyList<FamilyTypeCatalogColumn> columns,
        IReadOnlyList<string> fields)
    {
        FamilyManager manager = document.FamilyManager;
        manager.CurrentType = type;

        for (int index = 0; index < columns.Count && index + 1 < fields.Count; index++)
        {
            string value = fields[index + 1];
            if (value.Length == 0) continue;

            FamilyTypeCatalogColumn column = columns[index];
            FamilyParameter? parameter = FamilyTypeCatalogWriter.FindParameter(document, column.ParameterName);
            if (parameter is null || !FamilyTypeCatalogParameters.IsSetByCatalog(parameter)) continue;
            if (!TrySetValue(document, manager, parameter, value, column.Unit)) return false;
        }
        return true;
    }

    private static bool TrySetValue(
        Document document,
        FamilyManager manager,
        FamilyParameter parameter,
        string value,
        ForgeTypeId? unit)
    {
        switch (parameter.StorageType)
        {
            case StorageType.Double:
                if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
                    return false;
                manager.Set(parameter, unit is { } unitId ? UnitUtils.ConvertToInternalUnits(number, unitId) : number);
                return true;
            case StorageType.Integer:
                if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int integer))
                    return false;
                manager.Set(parameter, integer);
                return true;
            case StorageType.String:
                manager.Set(parameter, value);
                return true;
            case StorageType.ElementId:
                if (FindElementId(document, value) is not { } id) return false;
                manager.Set(parameter, id);
                return true;
            default:
                return true;
        }
    }

    // A catalog writes a family type as "Family : Type" and everything else, a material for example,
    // by name. The family document has to resolve that name back to an element.
    private static ElementId? FindElementId(Document document, string name)
    {
        Material? material = new FilteredElementCollector(document)
            .OfClass(typeof(Material))
            .Cast<Material>()
            .FirstOrDefault(candidate => string.Equals(candidate.Name, name, StringComparison.Ordinal));
        if (material is not null) return material.Id;

        FamilySymbol? symbol = new FilteredElementCollector(document)
            .OfClass(typeof(FamilySymbol))
            .Cast<FamilySymbol>()
            .FirstOrDefault(candidate =>
                string.Equals($"{candidate.Family.Name} : {candidate.Name}", name, StringComparison.Ordinal));
        return symbol?.Id;
    }
}
