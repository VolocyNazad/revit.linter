using Revit.Linter.PracticalTourPresenter.Infrastructure.Services;
using Revit.Linter.PracticalTourPresenter.Interactions.Abstractions.Models;

namespace Revit.Linter.PracticalTourPresenter.Tests;

/// <summary>
/// Covers the step a saved session resumes from, so a paused tour never asks for findings that the active
/// document no longer shows.
/// </summary>
public sealed class PracticalTourResumeTests
{
    [Theory]
    [InlineData(PracticalTourStep.OpenDocument)]
    [InlineData(PracticalTourStep.OpenConfigurationFolder)]
    [InlineData(PracticalTourStep.SearchAndFilters)]
    [InlineData(PracticalTourStep.SelectDiagnostic)]
    [InlineData(PracticalTourStep.RunDiagnostics)]
    public void Steps_before_the_run_resume_as_saved(PracticalTourStep savedStep)
    {
        Assert.Equal(
            savedStep,
            PracticalTourResume.ResolveStartStep(
                savedStep, hasOpenDocument: true, hasCleanRun: false, hasTrainingFindings: false));
    }

    [Theory]
    [InlineData(PracticalTourStep.InspectFinding)]
    [InlineData(PracticalTourStep.OpenHelp)]
    [InlineData(PracticalTourStep.SelectVisualization)]
    [InlineData(PracticalTourStep.FixList)]
    [InlineData(PracticalTourStep.IgnoreList)]
    [InlineData(PracticalTourStep.ExportReport)]
    [InlineData(PracticalTourStep.ExportReportAnotherFormat)]
    public void Finding_steps_resume_as_saved_when_the_document_still_shows_the_findings(
        PracticalTourStep savedStep)
    {
        Assert.Equal(
            savedStep,
            PracticalTourResume.ResolveStartStep(
                savedStep, hasOpenDocument: true, hasCleanRun: false, hasTrainingFindings: true));
    }

    [Theory]
    [InlineData(PracticalTourStep.InspectFinding)]
    [InlineData(PracticalTourStep.OpenHelp)]
    [InlineData(PracticalTourStep.SelectVisualization)]
    [InlineData(PracticalTourStep.FixList)]
    [InlineData(PracticalTourStep.IgnoreList)]
    [InlineData(PracticalTourStep.ExportReport)]
    [InlineData(PracticalTourStep.ExportReportAnotherFormat)]
    public void Finding_steps_restart_at_the_run_when_the_findings_are_gone(PracticalTourStep savedStep)
    {
        Assert.Equal(
            PracticalTourStep.RunDiagnostics,
            PracticalTourResume.ResolveStartStep(
                savedStep, hasOpenDocument: true, hasCleanRun: false, hasTrainingFindings: false));
    }

    [Fact]
    public void Clean_run_steps_resume_as_saved_because_they_expect_no_findings()
    {
        Assert.Equal(
            PracticalTourStep.ExportReport,
            PracticalTourResume.ResolveStartStep(
                PracticalTourStep.ExportReport, hasOpenDocument: true, hasCleanRun: true, hasTrainingFindings: false));
    }

    [Fact]
    public void Without_a_document_the_session_waits_for_one()
    {
        Assert.Equal(
            PracticalTourStep.OpenDocument,
            PracticalTourResume.ResolveStartStep(
                PracticalTourStep.InspectFinding, hasOpenDocument: false, hasCleanRun: false, hasTrainingFindings: true));
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

        PracticalTourProgress resumed = PracticalTourResume.ResolveStartProgress(
            saved, hasOpenDocument: true, hasTrainingFindings: false);

        Assert.Equal(PracticalTourStep.RunDiagnostics, resumed.CurrentStep);
        Assert.False(resumed.HasCleanRun);
        Assert.True(resumed.HasSingleVisualization);
        Assert.Equal(".csv", resumed.FirstExportFormat);
        Assert.Same(saved, PracticalTourResume.ResolveStartProgress(
            saved, hasOpenDocument: true, hasTrainingFindings: true));
    }
}
