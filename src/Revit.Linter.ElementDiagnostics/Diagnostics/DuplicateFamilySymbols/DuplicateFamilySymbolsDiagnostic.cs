using System.Globalization;
using Revit.Linter.DocumentQueries.Abstractions.Models;
using Revit.Linter.DocumentQueries.Abstractions.Services;
using Revit.Sugar;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.DuplicateFamilySymbols;

internal sealed class DuplicateFamilySymbolsDiagnostic(IDocumentQueryService documentQueries)
    : IElementDiagnostic
{
    private const string SymbolSignaturesQuery = "element-diagnostics:duplicate-family-symbol-signatures";

    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.DuplicateFamilySymbols;

    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
    {
        // Projects have no FamilyManager: symbols are compared through their own parameters,
        // like the two sides of a collision. The family counterpart SHRD0026 compares
        // FamilyManager types instead, which do not exist as elements.
        // Only symbols of the same family are duplicates: identical values in different
        // families are independent types, not copies.
        // Symbols and their signatures are read once per run into the transaction cache:
        // without it every target rescans the document and recomputes every signature (O(S^2)).
        var target = (FamilySymbol)targetElement;
        Dictionary<ElementId, string> signaturesBySymbolId = GetSignatures(document);
        if (!signaturesBySymbolId.TryGetValue(target.Id, out string? signature))
            signature = ReadSignature(target);
        ElementId targetFamilyId = target.Family.Id;
        string? duplicateName = null;
        foreach (FamilySymbol symbol in documentQueries.GetElementsOfClass<FamilySymbol>(document))
        {
            if (symbol.Id == target.Id || symbol.Family.Id != targetFamilyId)
                continue;
            if (!signaturesBySymbolId.TryGetValue(symbol.Id, out string? candidateSignature))
                candidateSignature = ReadSignature(symbol);
            if (candidateSignature == signature)
            {
                duplicateName = symbol.Name;
                break;
            }
        }
        return duplicateName is null
            ? new(DiagnosticVerdict.Valid)
            : new(DiagnosticVerdict.NotValid,
                new Dictionary<string, object> { ["duplicateNames"] = duplicateName });
    }

    private Dictionary<ElementId, string> GetSignatures(Document document)
        => documentQueries.GetOrCreate(
            DocumentQueryKey.Create(document, SymbolSignaturesQuery),
            () => BuildSignatures(document));

    private Dictionary<ElementId, string> BuildSignatures(Document document)
    {
        IReadOnlyList<FamilySymbol> symbols = documentQueries.GetElementsOfClass<FamilySymbol>(document);
        var signatures = new Dictionary<ElementId, string>(symbols.Count);
        foreach (FamilySymbol symbol in symbols)
            signatures[symbol.Id] = ReadSignature(symbol);
        return signatures;
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
