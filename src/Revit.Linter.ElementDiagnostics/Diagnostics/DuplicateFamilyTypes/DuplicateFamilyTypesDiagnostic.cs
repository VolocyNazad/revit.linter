using System.Globalization;
using Revit.Sugar;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.DuplicateFamilyTypes;

internal sealed class DuplicateFamilyTypesDiagnostic : IElementDiagnostic
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.DuplicateFamilyTypes;

    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
    {
        // Family types are not elements in a family document: the finding attaches to the
        // family itself and lists every duplicate pair. Twins report once, not per side.
        // Types are ordered by name so the reported pairs are deterministic.
        FamilyManager manager = document.FamilyManager;
        List<(FamilyType Type, string Signature)> signed = manager.Types
            .Cast<FamilyType>()
            .Where(type => !IsDefault(type))
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .Select(type => (type, ReadSignature(manager, type)))
            .ToList();
        List<string> pairs = [];
        for (int first = 0; first < signed.Count; first++)
            for (int second = first + 1; second < signed.Count; second++)
                if (signed[first].Signature == signed[second].Signature)
                    pairs.Add($"{signed[first].Type.Name} = {signed[second].Type.Name}");
        return pairs.Count == 0
            ? new(DiagnosticVerdict.Valid)
            : new(DiagnosticVerdict.NotValid,
                new Dictionary<string, object> { ["duplicateNames"] = string.Join("; ", pairs) });
    }

    // The default type usually has no name and is not shown in the UI: it takes part in no check.
    private static bool IsDefault(FamilyType type) => string.IsNullOrWhiteSpace(type.Name);

    // Compares every family parameter by stable name and value. The type name itself
    // is not a parameter, so no exclusion is needed: names differ by definition.
    // Values are read by storage type: the display string is not available for every
    // storage, and text set through the manager is not reflected in it.
    private static string ReadSignature(FamilyManager manager, FamilyType type) => string.Join(";",
        manager.Parameters
            .Cast<FamilyParameter>()
            .OrderBy(parameter => parameter.Definition.Name, StringComparer.Ordinal)
            .Select(parameter => $"{parameter.Definition.Name}={ReadValue(type, parameter)}"));

    private static string ReadValue(FamilyType type, FamilyParameter parameter) =>
        parameter.StorageType switch
        {
            StorageType.String => type.AsString(parameter) ?? "null",
            StorageType.Double => type.AsDouble(parameter) is double value
                ? value.ToString("R", CultureInfo.InvariantCulture)
                : "null",
            StorageType.Integer => type.AsInteger(parameter) is int intValue
                ? intValue.ToString("G", CultureInfo.InvariantCulture)
                : "null",
            StorageType.ElementId => type.AsElementId(parameter)?.Value().ToString("G", CultureInfo.InvariantCulture) ?? "null",
            _ => type.AsValueString(parameter) ?? "null",
        };
}
