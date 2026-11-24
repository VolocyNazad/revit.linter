using Revit.Linter.DocumentQueries;

namespace Revit.Linter.DocumentDiagnostics.Diagnostics.DuplicateProjectParameterNames;

// The category names go to the report as one list: the report quotes every name and folds a long
// set into an expander, which keeps a parameter bound to dozens of categories readable.
internal static class DuplicateProjectParameterNamesFormatting
{
    // Only a category bound to more than one definition of the name is a problem: the definitions
    // meet on that category, so an element of it carries two fields with one name. A category bound
    // to a single definition never meets the other definitions and stays out of the message.
    public static string[] DescribeSharedCategories(
        Document document, IEnumerable<ProjectParameterDefinition> definitions)
    {
        Dictionary<string, int> counts = new(StringComparer.Ordinal);
        foreach (ProjectParameterDefinition definition in definitions)
        {
            foreach (ElementId categoryId in definition.CategoryIds)
            {
                if (Category.GetCategory(document, categoryId)?.Name is not { } name) continue;
                counts[name] = counts.TryGetValue(name, out int count) ? count + 1 : 1;
            }
        }

        List<string> shared = [];
        foreach (KeyValuePair<string, int> pair in counts)
        {
            if (pair.Value > 1) shared.Add(pair.Key);
        }

        return [.. shared];
    }
}
