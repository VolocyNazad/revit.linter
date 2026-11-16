namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>Describes one finding that participates in the findings heat map.</summary>
/// <typeparam name="TKey">The element identity type; the aggregation stays independent of the Revit API.</typeparam>
/// <param name="Element">The identity of the finding target element.</param>
/// <param name="Severity">The severity of the finding.</param>
/// <param name="Code">The diagnostic code of the finding.</param>
/// <param name="IsObsolete">A value indicating whether the finding belongs to an obsolete diagnostic.</param>
public readonly record struct FindingHeatmapFinding<TKey>(
    TKey Element, DiagnosticSeverity Severity, string Code, bool IsObsolete = false)
    where TKey : notnull;

/// <summary>Describes the heat map value of one element.</summary>
/// <typeparam name="TKey">The element identity type.</typeparam>
/// <param name="Element">The identity of the element.</param>
/// <param name="Severity">The most severe severity among the element's findings.</param>
/// <param name="Count">The number of distinct diagnostic codes with that severity.</param>
public readonly record struct FindingHeatmapValue<TKey>(TKey Element, DiagnosticSeverity Severity, int Count)
    where TKey : notnull;

/// <summary>
/// Aggregates findings into one heat map value per element.
/// </summary>
/// <remarks>
/// Only the target element of a finding counts; its dependencies do not. An element with one error and
/// many warnings receives the error value with a count of one, because the warnings of a lower severity
/// never contribute to the colour. Obsolete findings are ignored, and a diagnostic code counts once per
/// element even when it produced several findings.
/// </remarks>
public static class FindingHeatmapAggregator
{
    /// <summary>Aggregates findings into one value per element.</summary>
    /// <typeparam name="TKey">The element identity type.</typeparam>
    /// <param name="findings">The findings to aggregate.</param>
    /// <returns>The values ordered by severity and count, both descending.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="findings"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<FindingHeatmapValue<TKey>> Aggregate<TKey>(
        IEnumerable<FindingHeatmapFinding<TKey>> findings)
        where TKey : notnull
    {
        if (findings is null) throw new ArgumentNullException(nameof(findings));

        Dictionary<TKey, (DiagnosticSeverity Severity, HashSet<string> Codes)> byElement = [];
        foreach (FindingHeatmapFinding<TKey> finding in findings)
        {
            if (finding.IsObsolete) continue;
            if (string.IsNullOrWhiteSpace(finding.Code)) continue;

            if (!byElement.TryGetValue(finding.Element, out var current))
            {
                byElement[finding.Element] = (finding.Severity, new HashSet<string>(StringComparer.Ordinal) { finding.Code });
                continue;
            }

            if (finding.Severity > current.Severity)
            {
                byElement[finding.Element] = (
                    finding.Severity, new HashSet<string>(StringComparer.Ordinal) { finding.Code });
            }
            else if (finding.Severity == current.Severity)
            {
                current.Codes.Add(finding.Code);
            }
        }

        return byElement
            .Select(pair => new FindingHeatmapValue<TKey>(pair.Key, pair.Value.Severity, pair.Value.Codes.Count))
            .OrderByDescending(value => value.Severity)
            .ThenByDescending(value => value.Count)
            .ToArray();
    }
}
