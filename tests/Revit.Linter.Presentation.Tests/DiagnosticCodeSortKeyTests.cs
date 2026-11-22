using Revit.Linter.Presentation.Models;

namespace Revit.Linter.Presentation.Tests;

/// <summary>
/// Verifies that a code column orders codes by their text and numbers, so a code with a larger number
/// comes after a smaller one instead of after a longer text.
/// </summary>
public sealed class DiagnosticCodeSortKeyTests
{
    [Theory]
    [InlineData("CSTM2", "CSTM10")]
    [InlineData("CSTM2A", "CSTM10A")]
    [InlineData("CSTM", "CSTM1")]
    [InlineData("CSTM1", "CSTM1A")]
    [InlineData("cstm2", "CSTM10")]
    [InlineData("A1B", "A10B")]
    public void Compare_places_the_smaller_code_first(string lower, string higher)
    {
        Assert.True(DiagnosticCodeSortKey.Compare(lower, higher) < 0);
        Assert.True(DiagnosticCodeSortKey.Compare(higher, lower) > 0);
    }

    [Fact]
    public void Compare_ignores_leading_zeros_when_it_reads_the_number()
    {
        Assert.True(DiagnosticCodeSortKey.Compare("CSTM001", "CSTM1") < 0);
        Assert.True(DiagnosticCodeSortKey.Compare("CSTM1", "CSTM001") > 0);
        Assert.True(DiagnosticCodeSortKey.Compare("CSTM002", "CSTM10") < 0);
    }

    [Fact]
    public void Compare_orders_numbers_that_do_not_fit_into_a_64_bit_integer()
    {
        Assert.True(DiagnosticCodeSortKey.Compare(
            "CSTM99999999999999999999", "CSTM100000000000000000000") < 0);
    }

    [Fact]
    public void Compare_places_a_missing_code_before_any_code()
    {
        Assert.True(DiagnosticCodeSortKey.Compare(null, "CSTM1") < 0);
        Assert.True(DiagnosticCodeSortKey.Compare("CSTM1", null) > 0);
        Assert.Equal(0, DiagnosticCodeSortKey.Compare(null, null));
    }

    [Fact]
    public void Compare_returns_zero_for_the_same_code()
    {
        Assert.Equal(0, DiagnosticCodeSortKey.Compare("CSTM10", "CSTM10"));
    }
}
