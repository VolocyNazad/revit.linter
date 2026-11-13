using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.Core.Tests;

public sealed class FindingHeatmapAggregatorTests
{
    private static FindingHeatmapFinding<int> Finding(
        int element, DiagnosticSeverity severity, string code, bool isObsolete = false) =>
        new(element, severity, code, isObsolete);

    [Fact]
    public void One_error_outweighs_many_warnings_of_the_same_element()
    {
        List<FindingHeatmapFinding<int>> findings = [Finding(7, DiagnosticSeverity.Error, "ERR001")];
        for (int index = 0; index < 50; index++)
            findings.Add(Finding(7, DiagnosticSeverity.Warning, $"WRN{index:D3}"));

        FindingHeatmapValue<int> value = Assert.Single(FindingHeatmapAggregator.Aggregate(findings));

        Assert.Equal(7, value.Element);
        Assert.Equal(DiagnosticSeverity.Error, value.Severity);
        Assert.Equal(1, value.Count);
    }

    [Fact]
    public void A_diagnostic_code_counts_once_per_element()
    {
        FindingHeatmapFinding<int>[] findings =
        [
            Finding(3, DiagnosticSeverity.Warning, "WRN001"),
            Finding(3, DiagnosticSeverity.Warning, "WRN001"),
            Finding(3, DiagnosticSeverity.Warning, "WRN002"),
        ];

        FindingHeatmapValue<int> value = Assert.Single(FindingHeatmapAggregator.Aggregate(findings));

        Assert.Equal(DiagnosticSeverity.Warning, value.Severity);
        Assert.Equal(2, value.Count);
    }

    [Fact]
    public void Warnings_of_an_element_with_a_worse_finding_do_not_raise_its_count()
    {
        FindingHeatmapFinding<int>[] findings =
        [
            Finding(1, DiagnosticSeverity.Warning, "WRN001"),
            Finding(1, DiagnosticSeverity.Error, "ERR001"),
            Finding(1, DiagnosticSeverity.Warning, "WRN002"),
        ];

        FindingHeatmapValue<int> value = Assert.Single(FindingHeatmapAggregator.Aggregate(findings));

        Assert.Equal(DiagnosticSeverity.Error, value.Severity);
        Assert.Equal(1, value.Count);
    }

    [Fact]
    public void Obsolete_findings_are_ignored()
    {
        FindingHeatmapFinding<int>[] findings =
        [
            Finding(2, DiagnosticSeverity.Error, "OLD001", isObsolete: true),
            Finding(2, DiagnosticSeverity.Warning, "WRN001"),
        ];

        FindingHeatmapValue<int> value = Assert.Single(FindingHeatmapAggregator.Aggregate(findings));

        Assert.Equal(DiagnosticSeverity.Warning, value.Severity);
        Assert.Equal(1, value.Count);
    }

    [Fact]
    public void Elements_are_ordered_by_severity_then_by_count()
    {
        FindingHeatmapFinding<int>[] findings =
        [
            Finding(1, DiagnosticSeverity.Warning, "WRN001"),
            Finding(1, DiagnosticSeverity.Warning, "WRN002"),
            Finding(2, DiagnosticSeverity.Error, "ERR001"),
            Finding(3, DiagnosticSeverity.Message, "MSG001"),
        ];

        IReadOnlyList<FindingHeatmapValue<int>> values = FindingHeatmapAggregator.Aggregate(findings);

        Assert.Equal([2, 1, 3], values.Select(value => value.Element));
        Assert.Equal([1, 2, 1], values.Select(value => value.Count));
    }

    [Fact]
    public void Findings_without_a_code_are_ignored()
    {
        FindingHeatmapFinding<int>[] findings = [Finding(4, DiagnosticSeverity.Error, " ")];

        Assert.Empty(FindingHeatmapAggregator.Aggregate(findings));
    }

    [Fact]
    public void Empty_input_produces_no_values()
    {
        Assert.Empty(FindingHeatmapAggregator.Aggregate<int>([]));
    }

    [Fact]
    public void Null_input_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => FindingHeatmapAggregator.Aggregate<int>(null!));
    }
}
