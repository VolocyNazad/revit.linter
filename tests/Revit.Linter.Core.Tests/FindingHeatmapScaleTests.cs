using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.Core.Tests;

public sealed class FindingHeatmapScaleTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    [InlineData(3, 2)]
    [InlineData(4, 2)]
    [InlineData(5, 3)]
    [InlineData(8, 3)]
    [InlineData(9, 4)]
    [InlineData(16, 4)]
    [InlineData(17, 5)]
    [InlineData(1000, 5)]
    public void Counts_map_to_fixed_bands(int count, int expectedLevel)
    {
        Assert.Equal(expectedLevel, FindingHeatmapScale.GetLevel(count));
    }

    [Fact]
    public void Bands_cover_every_positive_count_without_gaps()
    {
        IReadOnlyList<FindingHeatmapCountBand> bands = FindingHeatmapScale.CountBands;

        Assert.Equal(1, bands[0].MinimumCount);
        for (int index = 1; index < bands.Count; index++)
            Assert.Equal(bands[index - 1].MaximumCount + 1, bands[index].MinimumCount);
        Assert.Equal(int.MaxValue, bands[bands.Count - 1].MaximumCount);
    }

    [Theory]
    [InlineData(DiagnosticSeverity.Error)]
    [InlineData(DiagnosticSeverity.Warning)]
    [InlineData(DiagnosticSeverity.Message)]
    public void More_checks_never_darken_the_colour_within_one_severity(DiagnosticSeverity severity)
    {
        FindingHeatmapColor previous = FindingHeatmapScale.GetColor(severity, 1);
        for (int level = 1; level < FindingHeatmapScale.CountBands.Count; level++)
        {
            int count = FindingHeatmapScale.CountBands[level].MinimumCount;
            FindingHeatmapColor current = FindingHeatmapScale.GetColor(severity, count);

            Assert.True(current.Red >= previous.Red);
            Assert.True(current.Green >= previous.Green);
            Assert.True(current.Blue >= previous.Blue);
            previous = current;
        }
    }

    [Fact]
    public void Severities_use_the_same_colour_for_the_same_kind_of_check()
    {
        FindingHeatmapColor error = FindingHeatmapScale.GetColor(DiagnosticSeverity.Error, 1);
        FindingHeatmapColor warning = FindingHeatmapScale.GetColor(DiagnosticSeverity.Warning, 1);
        FindingHeatmapColor message = FindingHeatmapScale.GetColor(DiagnosticSeverity.Message, 1);

        Assert.NotEqual(error, warning);
        Assert.NotEqual(warning, message);
        Assert.True(error.Red > error.Green && error.Red > error.Blue);
        Assert.True(warning.Red > warning.Blue);
        Assert.True(message.Blue > message.Red);
    }
}
