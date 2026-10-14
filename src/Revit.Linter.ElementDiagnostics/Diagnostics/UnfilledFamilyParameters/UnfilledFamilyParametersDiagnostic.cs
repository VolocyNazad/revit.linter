namespace Revit.Linter.ElementDiagnostics.Diagnostics.UnfilledFamilyParameters;

internal sealed class UnfilledFamilyParametersDiagnostic : IElementDiagnostic
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.UnfilledFamilyParameters;

    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
    {
        // Family types are not elements in a family document: the finding attaches to the
        // family itself and lists every type with unfilled text cells. Only text cells have
        // a recognizable "unfilled" state: a zero number is a legitimate value.
        FamilyManager manager = document.FamilyManager;
        List<string> unfilled = [];
        foreach (FamilyType type in manager.Types.Cast<FamilyType>().Where(type => !IsDefault(type)))
        {
            string[] names = manager.Parameters
                .Cast<FamilyParameter>()
                .Where(parameter => IsUnfilledText(type, parameter))
                .Select(parameter => parameter.Definition.Name)
                .ToArray();
            if (names.Length > 0)
                unfilled.Add($"{type.Name}: {string.Join(", ", names)}");
        }
        return unfilled.Count == 0
            ? new(DiagnosticVerdict.Valid)
            : new(DiagnosticVerdict.NotValid,
                new Dictionary<string, object> { ["details"] = string.Join("; ", unfilled) });
    }

    private static bool IsUnfilledText(FamilyType type, FamilyParameter parameter)
    {
        if (parameter.StorageType != StorageType.String) return false;
        return string.IsNullOrWhiteSpace(type.AsString(parameter));
    }

    // The default type usually has no name and is not shown in the UI: it takes part in no check.
    private static bool IsDefault(FamilyType type) => string.IsNullOrWhiteSpace(type.Name);
}
