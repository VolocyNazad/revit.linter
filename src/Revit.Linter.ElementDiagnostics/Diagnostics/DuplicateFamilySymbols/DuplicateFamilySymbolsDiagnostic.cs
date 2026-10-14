using System.Globalization;
using Revit.Sugar;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.DuplicateFamilySymbols;

internal sealed class DuplicateFamilySymbolsDiagnostic : IElementDiagnostic
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.DuplicateFamilySymbols;

    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
    {
        // Projects have no FamilyManager: symbols are compared through their own parameters,
        // like the two sides of a collision. The family counterpart SHRD0026 compares
        // FamilyManager types instead, which do not exist as elements.
        // Only symbols of the same family are duplicates: identical values in different
        // families are independent types, not copies.
        var target = (FamilySymbol)targetElement;
        string signature = ReadSignature(target);
        string? duplicateName = new FilteredElementCollector(document)
            .OfClass(typeof(FamilySymbol))
            .Cast<FamilySymbol>()
            .Where(symbol => symbol.Id != target.Id
                && symbol.Family.Id == target.Family.Id
                && ReadSignature(symbol) == signature)
            .Select(symbol => symbol.Name)
            .FirstOrDefault();
        return duplicateName is null
            ? new(DiagnosticVerdict.Valid)
            : new(DiagnosticVerdict.NotValid,
                new Dictionary<string, object> { ["duplicateNames"] = duplicateName });
    }

    // Compares every parameter by stable identifier and storage value, except the type name
    // itself and the auto-generated Type IfcGUID: both differ by definition, so including
    // them would hide genuine duplicates.
    // Values are read by storage type: the display string is not available for every
    // storage, and text set on the symbol is not reflected in it.
    private static string ReadSignature(FamilySymbol symbol) => string.Join(";",
        symbol.Parameters
            .Cast<Parameter>()
            .Where(parameter => !IsIdentity(parameter))
            .OrderBy(parameter => parameter.Id.Value())
            .Select(parameter => $"{parameter.Id.Value()}={ReadValue(parameter)}"));

    private static string ReadValue(Parameter parameter)
    {
        if (!parameter.HasValue)
            return "null";
        return parameter.StorageType switch
        {
            StorageType.String => parameter.AsString() ?? "null",
            StorageType.Double => parameter.AsDouble().ToString("R", CultureInfo.InvariantCulture),
            StorageType.Integer => parameter.AsInteger().ToString("G", CultureInfo.InvariantCulture),
            StorageType.ElementId => parameter.AsElementId()?.Value().ToString("G", CultureInfo.InvariantCulture) ?? "null",
            _ => parameter.AsValueString() ?? "null",
        };
    }

    private static bool IsIdentity(Parameter parameter) =>
        parameter.Definition is InternalDefinition definition
        && (definition.BuiltInParameter == BuiltInParameter.ALL_MODEL_TYPE_NAME
            || definition.BuiltInParameter == BuiltInParameter.IFC_TYPE_GUID);
}
