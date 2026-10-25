using Revit.Linter.PracticalTourPresenter.Infrastructure.Services;

namespace Revit.Linter.PracticalTourPresenter.Tests;

public sealed class PracticalTourSelectionTests
{
    [Theory]
    [InlineData(new[] { "TOUR001" }, true)]
    [InlineData(new[] { "TOUR001", "SHRD001" }, false)]
    [InlineData(new[] { "SHRD001" }, false)]
    [InlineData(new string[0], false)]
    public void Selection_step_requires_the_training_check_to_be_the_only_enabled_diagnostic(
        string[] activeDiagnosticCodes,
        bool expected)
    {
        Assert.Equal(expected, PracticalTour.IsSinglePracticalTourDiagnostic(activeDiagnosticCodes));
    }
}
