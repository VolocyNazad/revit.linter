using Revit.Linter.WelcomePresenter.Abstractions.Models;
using Revit.Linter.WelcomePresenter.Infrastructure.Services;

namespace Revit.Linter.WelcomePresenter.Tests;

public sealed class PracticalTourStateMachineTests
{
    [Fact]
    public void Starts_with_document_selection_when_no_document_is_open()
    {
        PracticalTourStateMachine tour = new();

        tour.Start(hasOpenDocument: false);

        Assert.True(tour.IsActive);
        Assert.Equal(PracticalTourStep.OpenDocument, tour.CurrentStep);
    }

    [Fact]
    public void Skips_document_selection_when_a_document_is_already_open()
    {
        PracticalTourStateMachine tour = new();

        tour.Start(hasOpenDocument: true);

        Assert.Equal(PracticalTourStep.OpenConfigurationFolder, tour.CurrentStep);
    }

    [Fact]
    public void Ignores_an_action_that_is_not_the_current_step()
    {
        PracticalTourStateMachine tour = new();
        tour.Start(hasOpenDocument: true);

        bool advanced = tour.Observe(PracticalTourStep.ExportReport);

        Assert.False(advanced);
        Assert.Equal(PracticalTourStep.OpenConfigurationFolder, tour.CurrentStep);
    }

    [Fact]
    public void Completes_after_every_expected_action()
    {
        PracticalTourStateMachine tour = new();
        tour.Start(hasOpenDocument: true);

        foreach (PracticalTourStep step in new[]
                 {
                     PracticalTourStep.OpenConfigurationFolder,
                     PracticalTourStep.SearchAndFilters,
                     PracticalTourStep.SelectDiagnostic,
                     PracticalTourStep.RunDiagnostics,
                     PracticalTourStep.InspectFinding,
                     PracticalTourStep.ShowElement,
                     PracticalTourStep.NavigateFindings,
                     PracticalTourStep.UnderstandFix,
                     PracticalTourStep.FixList,
                 })
        {
            if (step == PracticalTourStep.ShowElement)
            {
                Assert.True(tour.ObserveVisualization(selectedFromMenu: false, availableOptionCount: 2));
                Assert.True(tour.ObserveVisualization(selectedFromMenu: true, availableOptionCount: 2));
            }
            else
            {
                Assert.True(tour.Observe(step));
            }
        }

        Assert.Equal(PracticalTourStep.ExportReport, tour.CurrentStep);
        Assert.True(tour.ObserveReportExport(".csv"));
        Assert.Equal(PracticalTourStep.ExportReportAnotherFormat, tour.CurrentStep);
        Assert.True(tour.ObserveReportExport(".html"));

        Assert.False(tour.IsActive);
        Assert.Equal(PracticalTourStep.Completed, tour.CurrentStep);
    }

    [Fact]
    public void Ignores_a_repeat_export_in_the_same_format()
    {
        PracticalTourStateMachine tour = new();
        tour.Start(hasOpenDocument: true, resumeStep: PracticalTourStep.ExportReport);

        Assert.True(tour.ObserveReportExport(".csv"));

        Assert.False(tour.ObserveReportExport(".CSV"));
        Assert.Equal(PracticalTourStep.ExportReportAnotherFormat, tour.CurrentStep);
        Assert.Equal(".csv", tour.FirstExportFormat);
    }

    [Fact]
    public void Advances_on_an_export_in_another_format()
    {
        PracticalTourStateMachine tour = new();
        tour.Start(hasOpenDocument: true, resumeStep: PracticalTourStep.ExportReport);

        Assert.True(tour.ObserveReportExport(".csv"));
        Assert.True(tour.ObserveReportExport(".json"));

        Assert.False(tour.IsActive);
        Assert.Equal(PracticalTourStep.Completed, tour.CurrentStep);
    }

    [Fact]
    public void Ignores_a_generic_observation_of_export_steps()
    {
        PracticalTourStateMachine tour = new();
        tour.Start(hasOpenDocument: true, resumeStep: PracticalTourStep.ExportReport);

        Assert.False(tour.Observe(PracticalTourStep.ExportReport));
        Assert.Equal(PracticalTourStep.ExportReport, tour.CurrentStep);
    }

    [Fact]
    public void Skips_visualization_selection_when_the_finding_has_only_one_option()
    {
        PracticalTourStateMachine tour = new();
        tour.Start(hasOpenDocument: true);
        Assert.True(tour.Observe(PracticalTourStep.OpenConfigurationFolder));
        Assert.True(tour.Observe(PracticalTourStep.SearchAndFilters));
        Assert.True(tour.Observe(PracticalTourStep.SelectDiagnostic));
        Assert.True(tour.Observe(PracticalTourStep.RunDiagnostics));
        Assert.True(tour.Observe(PracticalTourStep.InspectFinding));

        Assert.True(tour.ObserveVisualization(selectedFromMenu: false, availableOptionCount: 1));

        Assert.True(tour.HasSingleVisualization);
        Assert.Equal(PracticalTourStep.UnderstandFix, tour.CurrentStep);
    }

    [Fact]
    public void Advances_through_finding_navigation_only_on_the_expected_step()
    {
        PracticalTourStateMachine tour = new();
        tour.Start(hasOpenDocument: true);
        Assert.True(tour.Observe(PracticalTourStep.OpenConfigurationFolder));
        Assert.True(tour.Observe(PracticalTourStep.SearchAndFilters));
        Assert.True(tour.Observe(PracticalTourStep.SelectDiagnostic));
        Assert.True(tour.Observe(PracticalTourStep.RunDiagnostics));
        Assert.True(tour.Observe(PracticalTourStep.InspectFinding));
        Assert.True(tour.ObserveVisualization(selectedFromMenu: false, availableOptionCount: 2));
        Assert.True(tour.ObserveVisualization(selectedFromMenu: true, availableOptionCount: 2));

        Assert.False(tour.Observe(PracticalTourStep.UnderstandFix));
        Assert.True(tour.Observe(PracticalTourStep.NavigateFindings));

        Assert.Equal(PracticalTourStep.UnderstandFix, tour.CurrentStep);
    }

    [Fact]
    public void Advances_through_search_and_filters_only_on_the_expected_step()
    {
        PracticalTourStateMachine tour = new();
        tour.Start(hasOpenDocument: true);
        Assert.True(tour.Observe(PracticalTourStep.OpenConfigurationFolder));

        Assert.False(tour.Observe(PracticalTourStep.SelectDiagnostic));
        Assert.True(tour.Observe(PracticalTourStep.SearchAndFilters));

        Assert.Equal(PracticalTourStep.SelectDiagnostic, tour.CurrentStep);
    }

    [Fact]
    public void Advances_through_the_fix_list_only_on_the_expected_step()
    {
        PracticalTourStateMachine tour = new();
        tour.Start(hasOpenDocument: true);
        Assert.True(tour.Observe(PracticalTourStep.OpenConfigurationFolder));
        Assert.True(tour.Observe(PracticalTourStep.SearchAndFilters));
        Assert.True(tour.Observe(PracticalTourStep.SelectDiagnostic));
        Assert.True(tour.Observe(PracticalTourStep.RunDiagnostics));
        Assert.True(tour.Observe(PracticalTourStep.InspectFinding));
        Assert.True(tour.ObserveVisualization(selectedFromMenu: false, availableOptionCount: 2));
        Assert.True(tour.ObserveVisualization(selectedFromMenu: true, availableOptionCount: 2));
        Assert.True(tour.Observe(PracticalTourStep.NavigateFindings));
        Assert.True(tour.Observe(PracticalTourStep.UnderstandFix));

        Assert.False(tour.Observe(PracticalTourStep.ExportReport));
        Assert.True(tour.Observe(PracticalTourStep.FixList));

        Assert.Equal(PracticalTourStep.ExportReport, tour.CurrentStep);
    }

    [Fact]
    public void Stop_discards_the_active_session()
    {
        PracticalTourStateMachine tour = new();
        tour.Start(hasOpenDocument: false);

        tour.Stop();

        Assert.False(tour.IsActive);
        Assert.Equal(PracticalTourStep.Completed, tour.CurrentStep);
    }
}
