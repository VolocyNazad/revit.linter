using Revit.Linter.SizeTableDiagnostics.Diagnostics.SizeTables;

namespace Revit.Linter.SizeTableDiagnostics.Diagnostics.UnresolvedSizeTableArgument;

/// <summary>Finds formulas whose lookup table or column name cannot be determined statically.</summary>
internal sealed class UnresolvedSizeTableArgumentDiagnostic : IDocumentDiagnostic
{
    public DocumentDiagnosticId Identity => SizeTableDiagnosticIdCollector.UnresolvedSizeTableArgument;

    public IEnumerable<DiagnosticFeedback> Execute(Document targetDocument)
    {
        SizeTableReferenceCollection references = SizeTableReferenceCollector.Collect(targetDocument);
        HashSet<string> reported = new(StringComparer.OrdinalIgnoreCase);

        foreach (SizeLookupReference reference in references.LookupReferences)
        {
            if (!reference.TableResolved
                && reported.Add($"{reference.ParameterName}|table"))
            {
                yield return CreateFeedback(reference.ParameterName, "DOC007_tableArgument");
            }

            if (!reference.ColumnResolved
                && reported.Add($"{reference.ParameterName}|column"))
            {
                yield return CreateFeedback(reference.ParameterName, "DOC007_columnArgument");
            }
        }
    }

    private static DiagnosticFeedback CreateFeedback(
        string parameterName,
        string argumentResource) =>
        new(
            DiagnosticVerdict.NotValid,
            new Dictionary<string, object>
            {
                ["parameterName"] = parameterName,
                ["argument"] = SizeTableDiagnosticLocalizations.GetString(argumentResource)
            });
}
