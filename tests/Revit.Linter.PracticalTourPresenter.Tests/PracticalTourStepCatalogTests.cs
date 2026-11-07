using Revit.Linter.Localization;
using Revit.Linter.PracticalTourPresenter.Infrastructure.Services;
using Revit.Linter.PracticalTourPresenter.Interactions.Abstractions.Models;

namespace Revit.Linter.PracticalTourPresenter.Tests;

/// <summary>
/// Guards the single source of truth for the tour steps: the catalog must describe every step, list the
/// visible ones in tour order, and keep every text key resolvable for the current UI culture.
/// </summary>
public sealed class PracticalTourStepCatalogTests
{
    [Fact]
    public void Catalog_describes_every_step_exactly_once()
    {
        PracticalTourStep[] defined = PracticalTourStepCatalog.Definitions
            .Select(definition => definition.Step)
            .ToArray();
        PracticalTourStep[] expected = (PracticalTourStep[])Enum.GetValues(typeof(PracticalTourStep));

        Assert.Equal(expected.Length, defined.Length);
        Assert.Equal(
            expected.OrderBy(step => step).ToArray(),
            defined.OrderBy(step => step).ToArray());
    }

    [Fact]
    public void State_machine_order_comes_from_the_catalog()
    {
        Assert.Equal(
            PracticalTourStepCatalog.Definitions.Select(definition => definition.Step).ToArray(),
            PracticalTourStepCatalog.Steps);
        Assert.Equal(PracticalTourStep.Completed, PracticalTourStepCatalog.Steps[PracticalTourStepCatalog.Steps.Length - 1]);
    }

    [Fact]
    public void Visible_steps_exclude_the_completed_step_and_follow_the_catalog_order()
    {
        PracticalTourStep[] visible = PracticalTourStepCatalog.VisibleSteps
            .Select(definition => definition.Step)
            .ToArray();

        Assert.DoesNotContain(PracticalTourStep.Completed, visible);
        Assert.Equal(
            PracticalTourStepCatalog.Definitions
                .Where(definition => definition.IsVisible)
                .Select(definition => definition.Step)
                .ToArray(),
            visible);
    }

    [Fact]
    public void Every_visible_step_has_resolvable_step_texts()
    {
        foreach (PracticalTourStepDefinition definition in PracticalTourStepCatalog.VisibleSteps)
        {
            AssertResolvable(definition.TitleKey);
            AssertResolvable(definition.InstructionKey);
            AssertResolvable(definition.UnavailableTitleKey);
            AssertResolvable(definition.CleanRunInstructionKey);
            AssertResolvable(definition.TutorialSampleInstructionKey);
            AssertResolvable(definition.CheckpointInstructionKey);
        }
    }

    [Fact]
    public void Finding_dependent_steps_and_the_visualization_choice_are_marked()
    {
        PracticalTourStep[] findingDependent = PracticalTourStepCatalog.Definitions
            .Where(definition => definition.NeedsFindings)
            .Select(definition => definition.Step)
            .ToArray();
        PracticalTourStep[] visualizationChoice = PracticalTourStepCatalog.Definitions
            .Where(definition => definition.NeedsVisualizationChoice)
            .Select(definition => definition.Step)
            .ToArray();

        Assert.Equal(
            new[]
            {
                PracticalTourStep.InspectFinding,
                PracticalTourStep.OpenHelp,
                PracticalTourStep.ShowElement,
                PracticalTourStep.SelectVisualization,
                PracticalTourStep.NavigateFindings,
                PracticalTourStep.UnderstandFix,
                PracticalTourStep.UnderstandIgnore,
                PracticalTourStep.IgnoreList,
            },
            findingDependent);
        Assert.Equal([PracticalTourStep.SelectVisualization], visualizationChoice);
    }

    [Fact]
    public void Get_rejects_a_step_that_has_no_definition()
    {
        Assert.Throws<InvalidOperationException>(
            () => PracticalTourStepCatalog.Get((PracticalTourStep)int.MaxValue));
    }

    private static void AssertResolvable(string? resourceKey)
    {
        if (resourceKey is null) return;

        string value = LocalizationResourceReader.GetString(
            PracticalTourStepCatalog.ResourceBaseName, resourceKey);

        // The reader falls back to the key when the resource is missing, which would surface raw keys in
        // the pane; treat that as a broken catalog entry rather than a rendering detail.
        Assert.False(string.Equals(resourceKey, value, StringComparison.Ordinal));
        Assert.False(string.IsNullOrWhiteSpace(value));
    }
}
