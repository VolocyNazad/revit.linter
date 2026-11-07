using Revit.Linter.PracticalTourPresenter.Infrastructure.Services;
using Revit.Linter.PracticalTourPresenter.Interactions.Abstractions.Models;

namespace Revit.Linter.PracticalTourPresenter.Tests;

/// <summary>
/// Covers the step at which the tour narrows the published catalog to its training check, so the run, the
/// reports and the diagnostics list show that check alone while the user's own rules stay untouched.
/// </summary>
public sealed class PracticalTourScopeTests
{
    [Theory]
    [InlineData(PracticalTourStep.OpenDocument, false)]
    [InlineData(PracticalTourStep.OpenConfigurationFolder, false)]
    [InlineData(PracticalTourStep.SearchAndFilters, false)]
    [InlineData(PracticalTourStep.SelectDiagnostic, true)]
    [InlineData(PracticalTourStep.RunDiagnostics, true)]
    [InlineData(PracticalTourStep.InspectFinding, true)]
    [InlineData(PracticalTourStep.OpenHelp, true)]
    [InlineData(PracticalTourStep.ShowElement, true)]
    [InlineData(PracticalTourStep.SelectVisualization, true)]
    [InlineData(PracticalTourStep.NavigateFindings, true)]
    [InlineData(PracticalTourStep.UnderstandFix, true)]
    [InlineData(PracticalTourStep.FixList, true)]
    [InlineData(PracticalTourStep.UnderstandIgnore, true)]
    [InlineData(PracticalTourStep.IgnoreList, true)]
    [InlineData(PracticalTourStep.ExportReport, true)]
    [InlineData(PracticalTourStep.ExportReportAnotherFormat, true)]
    [InlineData(PracticalTourStep.Completed, false)]
    public void Training_scope_covers_the_steps_that_work_with_the_training_check(
        PracticalTourStep step,
        bool expected)
    {
        Assert.Equal(expected, PracticalTourStepCatalog.RequiresTrainingScope(step));
    }

    [Fact]
    public void Training_scope_turns_on_at_the_selection_step_and_stays_on_afterwards()
    {
        PracticalTourStateMachine tour = new();
        tour.Start(hasOpenDocument: true);

        Assert.False(PracticalTourStepCatalog.RequiresTrainingScope(tour.CurrentStep));

        Assert.True(tour.Observe(PracticalTourStep.OpenConfigurationFolder));
        Assert.False(tour.ObserveSearchText("TOUR001"));
        Assert.True(tour.Observe(PracticalTourStep.SearchAndFilters));

        Assert.Equal(PracticalTourStep.SelectDiagnostic, tour.CurrentStep);
        Assert.True(PracticalTourStepCatalog.RequiresTrainingScope(tour.CurrentStep));

        Assert.True(tour.Observe(PracticalTourStep.SelectDiagnostic));

        Assert.Equal(PracticalTourStep.RunDiagnostics, tour.CurrentStep);
        Assert.True(PracticalTourStepCatalog.RequiresTrainingScope(tour.CurrentStep));
    }

    [Fact]
    public void Resuming_a_later_step_needs_the_training_scope()
    {
        PracticalTourStateMachine tour = new();

        tour.Start(hasOpenDocument: true, resumeStep: PracticalTourStep.InspectFinding);

        Assert.True(PracticalTourStepCatalog.RequiresTrainingScope(tour.CurrentStep));
    }
}
