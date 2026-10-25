using Revit.Linter.PracticalTourPresenter.Infrastructure.Services;
using Revit.Linter.PracticalTourPresenter.Interactions.Abstractions.Models;

namespace Revit.Linter.PracticalTourPresenter.Tests;

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

        // The search-and-filters step needs typed text as well as a filter change.
        Assert.False(tour.ObserveSearchText("TOUR001"));

        foreach (PracticalTourStep step in new[]
                 {
                     PracticalTourStep.OpenConfigurationFolder,
                     PracticalTourStep.SearchAndFilters,
                     PracticalTourStep.SelectDiagnostic,
                     PracticalTourStep.RunDiagnostics,
                     PracticalTourStep.InspectFinding,
                     PracticalTourStep.OpenHelp,
                     PracticalTourStep.ShowElement,
                     PracticalTourStep.NavigateFindings,
                     PracticalTourStep.UnderstandFix,
                     PracticalTourStep.FixList,
                     PracticalTourStep.UnderstandIgnore,
                     PracticalTourStep.IgnoreList,
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
        // The search-and-filters step needs typed text as well as a filter change.
        Assert.False(tour.ObserveSearchText("TOUR001"));
        Assert.True(tour.Observe(PracticalTourStep.SearchAndFilters));
        Assert.True(tour.Observe(PracticalTourStep.SelectDiagnostic));
        Assert.True(tour.Observe(PracticalTourStep.RunDiagnostics));
        Assert.True(tour.Observe(PracticalTourStep.InspectFinding));
        Assert.True(tour.Observe(PracticalTourStep.OpenHelp));

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
        // The search-and-filters step needs typed text as well as a filter change.
        Assert.False(tour.ObserveSearchText("TOUR001"));
        Assert.True(tour.Observe(PracticalTourStep.SearchAndFilters));
        Assert.True(tour.Observe(PracticalTourStep.SelectDiagnostic));
        Assert.True(tour.Observe(PracticalTourStep.RunDiagnostics));
        Assert.True(tour.Observe(PracticalTourStep.InspectFinding));
        Assert.True(tour.Observe(PracticalTourStep.OpenHelp));
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
        // The search-and-filters step needs typed text as well as a filter change.
        Assert.False(tour.ObserveSearchText("TOUR001"));
        Assert.True(tour.Observe(PracticalTourStep.SearchAndFilters));

        Assert.Equal(PracticalTourStep.SelectDiagnostic, tour.CurrentStep);
    }

    [Fact]
    public void Advances_through_the_fix_list_only_on_the_expected_step()
    {
        PracticalTourStateMachine tour = new();
        tour.Start(hasOpenDocument: true);
        Assert.True(tour.Observe(PracticalTourStep.OpenConfigurationFolder));
        // The search-and-filters step needs typed text as well as a filter change.
        Assert.False(tour.ObserveSearchText("TOUR001"));
        Assert.True(tour.Observe(PracticalTourStep.SearchAndFilters));
        Assert.True(tour.Observe(PracticalTourStep.SelectDiagnostic));
        Assert.True(tour.Observe(PracticalTourStep.RunDiagnostics));
        Assert.True(tour.Observe(PracticalTourStep.InspectFinding));
        Assert.True(tour.Observe(PracticalTourStep.OpenHelp));
        Assert.True(tour.ObserveVisualization(selectedFromMenu: false, availableOptionCount: 2));
        Assert.True(tour.ObserveVisualization(selectedFromMenu: true, availableOptionCount: 2));
        Assert.True(tour.Observe(PracticalTourStep.NavigateFindings));
        Assert.True(tour.Observe(PracticalTourStep.UnderstandFix));

        Assert.False(tour.Observe(PracticalTourStep.UnderstandIgnore));
        Assert.False(tour.Observe(PracticalTourStep.ExportReport));
        Assert.True(tour.Observe(PracticalTourStep.FixList));

        Assert.False(tour.Observe(PracticalTourStep.IgnoreList));
        Assert.False(tour.Observe(PracticalTourStep.ExportReport));
        Assert.True(tour.Observe(PracticalTourStep.UnderstandIgnore));

        Assert.Equal(PracticalTourStep.IgnoreList, tour.CurrentStep);
    }

    [Fact]
    public void Advances_through_help_only_on_the_expected_step()
    {
        PracticalTourStateMachine tour = new();
        tour.Start(hasOpenDocument: true);
        Assert.True(tour.Observe(PracticalTourStep.OpenConfigurationFolder));
        // The search-and-filters step needs typed text as well as a filter change.
        Assert.False(tour.ObserveSearchText("TOUR001"));
        Assert.True(tour.Observe(PracticalTourStep.SearchAndFilters));
        Assert.True(tour.Observe(PracticalTourStep.SelectDiagnostic));
        Assert.True(tour.Observe(PracticalTourStep.RunDiagnostics));

        Assert.False(tour.Observe(PracticalTourStep.OpenHelp));
        Assert.True(tour.Observe(PracticalTourStep.InspectFinding));

        Assert.False(tour.Observe(PracticalTourStep.ShowElement));
        Assert.True(tour.Observe(PracticalTourStep.OpenHelp));

        Assert.Equal(PracticalTourStep.ShowElement, tour.CurrentStep);
    }

    [Fact]
    public void Advances_through_ignore_steps_only_on_the_expected_step()
    {
        PracticalTourStateMachine tour = new();
        tour.Start(hasOpenDocument: true);
        Assert.True(tour.Observe(PracticalTourStep.OpenConfigurationFolder));
        // The search-and-filters step needs typed text as well as a filter change.
        Assert.False(tour.ObserveSearchText("TOUR001"));
        Assert.True(tour.Observe(PracticalTourStep.SearchAndFilters));
        Assert.True(tour.Observe(PracticalTourStep.SelectDiagnostic));
        Assert.True(tour.Observe(PracticalTourStep.RunDiagnostics));
        Assert.True(tour.Observe(PracticalTourStep.InspectFinding));
        Assert.True(tour.Observe(PracticalTourStep.OpenHelp));
        Assert.True(tour.ObserveVisualization(selectedFromMenu: false, availableOptionCount: 2));
        Assert.True(tour.ObserveVisualization(selectedFromMenu: true, availableOptionCount: 2));
        Assert.True(tour.Observe(PracticalTourStep.NavigateFindings));

        Assert.False(tour.Observe(PracticalTourStep.UnderstandIgnore));
        Assert.True(tour.Observe(PracticalTourStep.UnderstandFix));

        Assert.False(tour.Observe(PracticalTourStep.UnderstandIgnore));
        Assert.True(tour.Observe(PracticalTourStep.FixList));

        Assert.False(tour.Observe(PracticalTourStep.IgnoreList));
        Assert.True(tour.Observe(PracticalTourStep.UnderstandIgnore));

        Assert.False(tour.Observe(PracticalTourStep.ExportReport));
        Assert.True(tour.Observe(PracticalTourStep.IgnoreList));

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

    [Fact]
    public void Requires_typed_text_and_a_filter_change_for_search_and_filters()
    {
        PracticalTourStateMachine filterFirst = new();
        filterFirst.Start(hasOpenDocument: true);
        Assert.True(filterFirst.Observe(PracticalTourStep.OpenConfigurationFolder));

        Assert.False(filterFirst.Observe(PracticalTourStep.SearchAndFilters));
        Assert.False(filterFirst.ObserveSearchText(string.Empty));
        Assert.True(filterFirst.ObserveSearchText("TOUR001"));

        Assert.Equal(PracticalTourStep.SelectDiagnostic, filterFirst.CurrentStep);

        PracticalTourStateMachine textFirst = new();
        textFirst.Start(hasOpenDocument: true);

        // Typing already on the previous step arms the text half in advance.
        Assert.False(textFirst.ObserveSearchText("TOUR001"));
        Assert.True(textFirst.Observe(PracticalTourStep.OpenConfigurationFolder));
        Assert.True(textFirst.Observe(PracticalTourStep.SearchAndFilters));

        Assert.Equal(PracticalTourStep.SelectDiagnostic, textFirst.CurrentStep);
    }

    [Fact]
    public void Clearing_the_search_box_disarms_the_text_half()
    {
        PracticalTourStateMachine tour = new();
        tour.Start(hasOpenDocument: true);
        Assert.True(tour.Observe(PracticalTourStep.OpenConfigurationFolder));

        Assert.False(tour.ObserveSearchText("TOUR001"));
        Assert.False(tour.ObserveSearchText("  "));
        Assert.False(tour.Observe(PracticalTourStep.SearchAndFilters));
        Assert.True(tour.ObserveSearchText("TOUR001"));

        Assert.Equal(PracticalTourStep.SelectDiagnostic, tour.CurrentStep);
    }
}
