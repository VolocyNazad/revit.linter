namespace Revit.Linter.ElementDiagnostics.Diagnostics.UnsharedFamilyParameters;

internal sealed class UnsharedFamilyParametersDiagnostic : IElementDiagnostic
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.UnsharedFamilyParameters;

    // A family parameter that is not shared stays inside the family: a project schedule and a tag
    // cannot show it. The check names such parameters before the family is loaded, so the user can
    // turn them into shared parameters instead of learning it after the fact.
    public IReadOnlyList<DiagnosticFeedback> Execute(Document document, View? view, Element targetElement)
    {
        FamilyManager manager = document.FamilyManager;
        List<string> names = [];
        foreach (FamilyParameter parameter in manager.Parameters)
        {
            if (parameter.IsShared) continue;
            if (!IsUserDefined(parameter)) continue;

            string name = ReadName(parameter);
            if (string.IsNullOrWhiteSpace(name)) continue;
            names.Add(name);
        }

        if (names.Count == 0) return DiagnosticFeedbacks.Valid;

        // The names are handed over as a list, not as one joined string: the report quotes every name
        // and folds a long list into an expander, so a family with many parameters stays readable.
        return DiagnosticFeedbacks.Of(new DiagnosticFeedback(
            DiagnosticVerdict.NotValid,
            new Dictionary<string, object>
            {
                ["parameter.count"] = names.Count,
                ["parameter.names"] = names,
            }));
    }

    // Built-in parameters are not user-defined: they ship with Revit and cannot become shared.
    private static bool IsUserDefined(FamilyParameter parameter) =>
        parameter.Definition is InternalDefinition definition
        && definition.BuiltInParameter == BuiltInParameter.INVALID;

    // Revit can refuse to name a parameter of a strange family: the name is then empty and the
    // parameter is skipped instead of failing the whole run.
    private static string ReadName(FamilyParameter parameter)
    {
        try
        {
            return parameter.Definition.Name;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}
