namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyTypeCatalog;

/// <summary>Reads the type catalog spec and unit strings of a family parameter.</summary>
internal static class FamilyTypeCatalogUnits
{
    private static readonly Lazy<IReadOnlyDictionary<string, ForgeTypeId>> UnitsByToken = new(BuildUnitsByToken);

    /// <summary>Gets the catalog spec and unit strings, or <c>OTHER</c> when the spec is not measurable.</summary>
    /// <param name="parameter">The family parameter.</param>
    /// <returns>The spec string, the unit string and the unit identifier used to convert values.</returns>
    public static (string Spec, string Unit, ForgeTypeId? UnitId) Read(FamilyParameter parameter)
    {
        ForgeTypeId spec = ReadSpec(parameter.Definition);
#if IS2021
        // In 2021 the method returns NUMBER or OTHER for a parameter a catalog cannot measure
        // instead of refusing the spec, so those tokens mean OTHER.
        string specToken = UnitUtils.GetTypeCatalogStringForSpec(spec);
        if (specToken.Length == 0
            || string.Equals(specToken, "NUMBER", StringComparison.Ordinal)
            || string.Equals(specToken, "OTHER", StringComparison.Ordinal))
        {
            return ("OTHER", string.Empty, null);
        }
#else
        if (!UnitUtils.IsMeasurableSpec(spec)) return ("OTHER", string.Empty, null);

        string specToken = UnitUtils.GetTypeCatalogStringForSpec(spec);
        if (specToken.Length == 0) return ("OTHER", string.Empty, null);
#endif

        try
        {
            ForgeTypeId unitId = parameter.GetUnitTypeId();
            string unitToken = UnitUtils.GetTypeCatalogStringForUnit(unitId);
            // Currency names the spec as its own unit; the catalog writes the spec only.
            if (unitToken.Length == 0 || string.Equals(unitToken, specToken, StringComparison.Ordinal))
                return (specToken, string.Empty, null);
            return (specToken, unitToken, unitId);
        }
        catch (Exception exception) when (exception is Autodesk.Revit.Exceptions.InvalidOperationException
            or Autodesk.Revit.Exceptions.ArgumentException
            or InvalidOperationException
            or ArgumentException)
        {
            // A measurable spec without a unit (currency, for example) has a spec but no unit; a unit
            // a catalog cannot name leaves the column without one too.
            return (specToken, string.Empty, null);
        }
    }

    /// <summary>Finds the unit a catalog header names by its token.</summary>
    /// <param name="token">The token after the second <c>##</c>, for example <c>MILLIMETERS</c>.</param>
    /// <returns>The unit identifier, or <see langword="null"/> when the token is not a unit.</returns>
    public static ForgeTypeId? FindUnit(string token) =>
        token.Length > 0 && UnitsByToken.Value.TryGetValue(token, out ForgeTypeId? unit) ? unit : null;

    private static IReadOnlyDictionary<string, ForgeTypeId> BuildUnitsByToken()
    {
        Dictionary<string, ForgeTypeId> units = new(StringComparer.Ordinal);
        foreach (ForgeTypeId unit in UnitUtils.GetAllUnits())
        {
            string token = UnitUtils.GetTypeCatalogStringForUnit(unit);
            if (token.Length > 0) units[token] = unit;
        }
        return units;
    }

    private static ForgeTypeId ReadSpec(Definition definition)
    {
#if IS2021
        return definition.GetSpecTypeId();
#else
        return definition.GetDataType();
#endif
    }

}
