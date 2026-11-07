using Revit.Linter.PracticalTourPresenter.Infrastructure.Services;
using Revit.Linter.PracticalTourPresenter.Interactions.Abstractions.Models;

namespace Revit.Linter.PracticalTourPresenter.Tests;

/// <summary>
/// Covers the checkpoint a saved session resumes from: the run of the training check. Steps after it are
/// replayed because their findings are not kept across a pause; steps before it keep their position.
/// </summary>
public sealed class PracticalTourResumeTests
{
    [Theory]
    [InlineData(PracticalTourStep.OpenDocument)]
    [InlineData(PracticalTourStep.OpenConfigurationFolder)]
    [InlineData(PracticalTourStep.SearchAndFilters)]
    [InlineData(PracticalTourStep.SelectDiagnostic)]
    [InlineData(PracticalTourStep.RunDiagnostics)]
    public void Steps_up_to_the_run_resume_as_saved(PracticalTourStep savedStep)
    {
        Assert.Equal(
            savedStep,
            PracticalTourResume.ResolveStartStep(savedStep, hasOpenDocument: true));
    }

    [Theory]
    [InlineData(PracticalTourStep.InspectFinding)]
    [InlineData(PracticalTourStep.OpenHelp)]
    [InlineData(PracticalTourStep.ShowElement)]
    [InlineData(PracticalTourStep.SelectVisualization)]
    [InlineData(PracticalTourStep.NavigateFindings)]
    [InlineData(PracticalTourStep.UnderstandFix)]
    [InlineData(PracticalTourStep.FixList)]
    [InlineData(PracticalTourStep.UnderstandIgnore)]
    [InlineData(PracticalTourStep.IgnoreList)]
    [InlineData(PracticalTourStep.ExportReport)]
    [InlineData(PracticalTourStep.ExportReportAnotherFormat)]
    public void Steps_after_the_run_restart_at_the_run_checkpoint(PracticalTourStep savedStep)
    {
        Assert.Equal(
            PracticalTourStep.RunDiagnostics,
            PracticalTourResume.ResolveStartStep(savedStep, hasOpenDocument: true));
    }

    [Fact]
    public void Without_a_document_the_session_waits_for_one()
    {
        Assert.Equal(
            PracticalTourStep.OpenDocument,
            PracticalTourResume.ResolveStartStep(PracticalTourStep.InspectFinding, hasOpenDocument: false));
    }

    [Fact]
    public void Resolved_progress_keeps_the_session_facts()
    {
        PracticalTourProgress saved = new()
        {
            CurrentStep = PracticalTourStep.InspectFinding,
            HasCleanRun = false,
            HasSingleVisualization = true,
            FirstExportFormat = ".csv",
        };

        PracticalTourProgress resumed = PracticalTourResume.ResolveStartProgress(saved, hasOpenDocument: true);

        Assert.Equal(PracticalTourStep.RunDiagnostics, resumed.CurrentStep);
        Assert.False(resumed.HasCleanRun);
        Assert.True(resumed.HasSingleVisualization);
        Assert.Equal(".csv", resumed.FirstExportFormat);
    }

    [Fact]
    public void Progress_at_the_checkpoint_is_returned_unchanged()
    {
        PracticalTourProgress saved = new() { CurrentStep = PracticalTourStep.RunDiagnostics };

        Assert.Same(saved, PracticalTourResume.ResolveStartProgress(saved, hasOpenDocument: true));
    }
}
