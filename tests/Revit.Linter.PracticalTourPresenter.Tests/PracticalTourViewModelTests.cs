using Revit.Linter.Localization;
using Revit.Linter.PracticalTourPresenter.Infrastructure.Services;
using Revit.Linter.PracticalTourPresenter.Interactions.Abstractions.Models;
using Revit.Linter.PracticalTourPresenter.ViewModels;

namespace Revit.Linter.PracticalTourPresenter.Tests;

/// <summary>
/// Covers what the pane shows for each step: the row order, the completed and current marks, the
/// unavailable rows and the texts the session selects. The catalog tests prove that the table is
/// complete; these tests prove that the pane reads it as intended.
/// </summary>
public sealed class PracticalTourViewModelTests
{
    [Fact]
    public void Rows_follow_the_catalog_order_and_resolve_their_texts()
    {
        PracticalTourViewModel viewModel = Start(hasOpenDocument: true);

        Assert.Equal(PracticalTourStepCatalog.VisibleSteps.Length, viewModel.Steps.Count);

        for (int index = 0; index < PracticalTourStepCatalog.VisibleSteps.Length; index++)
        {
            PracticalTourStepDefinition definition = PracticalTourStepCatalog.VisibleSteps[index];
            Assert.Equal(Resolve(definition.TitleKey!), viewModel.Steps[index].Title);
            Assert.Equal(Resolve(definition.InstructionKey!), viewModel.Steps[index].Instruction);
        }
    }

    [Fact]
    public void Current_step_is_marked_and_earlier_steps_are_completed()
    {
        PracticalTourViewModel viewModel = Start(hasOpenDocument: true);

        Assert.Equal(PracticalTourStep.OpenConfigurationFolder, viewModel.CurrentStep);
        int currentIndex = IndexOfVisibleStep(PracticalTourStep.OpenConfigurationFolder);

        for (int index = 0; index < viewModel.Steps.Count; index++)
        {
            Assert.Equal(index == currentIndex, viewModel.Steps[index].IsCurrent);
            Assert.Equal(index < currentIndex, viewModel.Steps[index].IsCompleted);
        }
    }

    [Fact]
    public void Clean_run_marks_finding_steps_unavailable_and_uses_the_clean_run_instruction()
    {
        PracticalTourViewModel viewModel = Start(hasOpenDocument: true);
        ReachSelection(viewModel);

        viewModel.ObserveDiagnosticRun(hasFindings: false);

        Assert.Equal(PracticalTourStep.ExportReport, viewModel.CurrentStep);
        Assert.Equal(Resolve("cleanRunExportReport_text"), viewModel.Instruction);

        foreach (PracticalTourStep step in new[]
                 {
                     PracticalTourStep.InspectFinding,
                     PracticalTourStep.OpenHelp,
                     PracticalTourStep.ShowElement,
                     PracticalTourStep.SelectVisualization,
                     PracticalTourStep.NavigateFindings,
                     PracticalTourStep.UnderstandFix,
                     PracticalTourStep.UnderstandIgnore,
                     PracticalTourStep.IgnoreList,
                 })
        {
            Assert.True(RowFor(viewModel, step).IsUnavailable);
        }

        Assert.False(RowFor(viewModel, PracticalTourStep.FixList).IsUnavailable);
    }

    [Fact]
    public void Single_visualization_marks_the_choice_step_unavailable_with_its_own_title()
    {
        PracticalTourViewModel viewModel = Start(hasOpenDocument: true);
        ReachShowElement(viewModel);

        viewModel.ObserveVisualization(selectedFromMenu: false, availableOptionCount: 1);

        Assert.Equal(PracticalTourStep.UnderstandFix, viewModel.CurrentStep);
        PracticalTourStepRow row = RowFor(viewModel, PracticalTourStep.SelectVisualization);
        Assert.True(row.IsUnavailable);
        Assert.Equal(Resolve("selectVisualizationUnavailableStep_text"), row.Title);
    }

    [Fact]
    public void Tutorial_sample_instruction_replaces_the_open_document_text()
    {
        PracticalTourViewModel viewModel = Start(hasOpenDocument: false, tutorialSampleQueued: true);

        Assert.Equal(PracticalTourStep.OpenDocument, viewModel.CurrentStep);
        Assert.Equal(Resolve("openTutorialSample_text"), viewModel.Instruction);
        Assert.Equal(Resolve("openTutorialSample_text"), RowFor(viewModel, PracticalTourStep.OpenDocument).Instruction);
    }

    [Fact]
    public void Completion_leaves_every_row_completed_and_no_current_row()
    {
        PracticalTourViewModel viewModel = Start(hasOpenDocument: true);
        ReachShowElement(viewModel);
        viewModel.ObserveVisualization(selectedFromMenu: false, availableOptionCount: 2);
        viewModel.ObserveVisualization(selectedFromMenu: true, availableOptionCount: 2);
        viewModel.Observe(PracticalTourStep.NavigateFindings);
        viewModel.Observe(PracticalTourStep.UnderstandFix);
        viewModel.Observe(PracticalTourStep.FixList);
        viewModel.Observe(PracticalTourStep.UnderstandIgnore);
        viewModel.Observe(PracticalTourStep.IgnoreList);
        viewModel.ObserveReportExport(".csv");
        viewModel.ObserveReportExport(".html");

        Assert.Equal(PracticalTourStep.Completed, viewModel.CurrentStep);
        Assert.Equal(Resolve("completed_text"), viewModel.Instruction);
        Assert.All(viewModel.Steps, row => Assert.True(row.IsCompleted));
        Assert.All(viewModel.Steps, row => Assert.False(row.IsCurrent));
    }

    private static PracticalTourViewModel Start(bool hasOpenDocument, bool tutorialSampleQueued = false)
    {
        PracticalTourViewModel viewModel = new(new PracticalTourStateMachine());
        viewModel.Start(hasOpenDocument, progress: null, tutorialSampleQueued);
        return viewModel;
    }

    private static void ReachSelection(PracticalTourViewModel viewModel)
    {
        viewModel.Observe(PracticalTourStep.OpenConfigurationFolder);
        viewModel.ObserveSearchText("TOUR001");
        viewModel.Observe(PracticalTourStep.SearchAndFilters);
        viewModel.Observe(PracticalTourStep.SelectDiagnostic);
    }

    private static void ReachShowElement(PracticalTourViewModel viewModel)
    {
        ReachSelection(viewModel);
        viewModel.ObserveDiagnosticRun(hasFindings: true);
        viewModel.Observe(PracticalTourStep.InspectFinding);
        viewModel.Observe(PracticalTourStep.OpenHelp);
    }

    private static PracticalTourStepRow RowFor(PracticalTourViewModel viewModel, PracticalTourStep step) =>
        viewModel.Steps[IndexOfVisibleStep(step)];

    private static int IndexOfVisibleStep(PracticalTourStep step) =>
        Array.FindIndex(PracticalTourStepCatalog.VisibleSteps, definition => definition.Step == step);

    private static string Resolve(string resourceKey) =>
        LocalizationResourceReader.GetString(PracticalTourStepCatalog.ResourceBaseName, resourceKey);
}
