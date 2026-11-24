using Revit.Linter.DocumentQueries;
using Revit.Linter.DocumentQueries.Abstractions.Services;

namespace Revit.Linter.DocumentDiagnostics.Diagnostics.DuplicateProjectParameterNames;

internal sealed class DuplicateProjectParameterNamesDiagnostic(IDocumentQueryService documentQueries)
    : IDocumentDiagnostic
{
    public DocumentDiagnosticId Identity => DocumentDiagnosticIdCollector.DuplicateProjectParameterNames;

    public IEnumerable<DiagnosticFeedback> Execute(Document targetDocument)
    {
        // A project parameter definition is stored in the binding map of the document, so a name
        // that repeats there is two different definitions the user sees as two fields: the
        // Properties palette and a schedule show both. Family parameters are not in the map; the
        // element check SHRD0044 covers them.
        // One finding per repeated name: the name is the problem, and the row lists the categories
        // the definitions are bound to as one list, so a long category set is quoted and folded by
        // the report instead of stretching the row.
        ProjectParameterCatalog catalog = ProjectParameterCatalog.Get(documentQueries, targetDocument);
        foreach (IGrouping<string, ProjectParameterDefinition> group in catalog.Definitions
            .GroupBy(definition => definition.Name, StringComparer.Ordinal)
            .Where(group => group.Count() > 1))
        {
            string[] categories = DuplicateProjectParameterNamesFormatting.DescribeSharedCategories(
                targetDocument, group);
            yield return new DiagnosticFeedback(
                DiagnosticVerdict.NotValid,
                new Dictionary<string, object>
                {
                    ["parameter.name"] = group.Key,
                    ["parameter.count"] = group.Count(),
                    ["parameter.categories"] = categories,
                })
            {
                MessageFormat = categories.Length == 0
                    ? DocumentDiagnosticLocalizations.GetString("DOC004_noOverlapMessage")
                    : null,
            };
        }
    }
}
