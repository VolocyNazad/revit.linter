using Revit.Linter.WelcomePresenter.Abstractions.Models;
using Revit.Linter.WelcomePresenter.Infrastructure.Services;

namespace Revit.Linter.WelcomePresenter.Tests;

public sealed class WelcomeWizardStateTests
{
    private const int WizardVersion = 1;
    private const int RevitVersion = 2025;

    [Fact]
    public void New_user_gets_every_step()
    {
        WelcomeWizardPlan plan = WelcomeWizardState.GetPendingPlan(new WelcomeSettings(), WizardVersion, RevitVersion);

        Assert.True(plan.IncludeIntro);
        Assert.True(plan.IncludeExamples);
        Assert.True(plan.HasSteps);
    }

    [Fact]
    public void Nothing_is_pending_after_the_wizard_was_shown()
    {
        WelcomeSettings settings = new();
        WelcomeWizardState.MarkShown(settings, WelcomeWizardPlan.AllSteps, WizardVersion, RevitVersion);

        WelcomeWizardPlan plan = WelcomeWizardState.GetPendingPlan(settings, WizardVersion, RevitVersion);

        Assert.False(plan.HasSteps);
    }

    [Fact]
    public void Another_revit_version_gets_only_the_examples_step()
    {
        WelcomeSettings settings = new();
        WelcomeWizardState.MarkShown(settings, WelcomeWizardPlan.AllSteps, WizardVersion, 2023);

        WelcomeWizardPlan plan = WelcomeWizardState.GetPendingPlan(settings, WizardVersion, RevitVersion);

        Assert.False(plan.IncludeIntro);
        Assert.True(plan.IncludeExamples);
    }

    [Fact]
    public void Newer_wizard_version_gets_only_the_introduction_again()
    {
        WelcomeSettings settings = new();
        WelcomeWizardState.MarkShown(settings, WelcomeWizardPlan.AllSteps, WizardVersion, RevitVersion);

        WelcomeWizardPlan plan = WelcomeWizardState.GetPendingPlan(settings, WizardVersion + 1, RevitVersion);

        Assert.True(plan.IncludeIntro);
        Assert.False(plan.IncludeExamples);
    }

    [Fact]
    public void Marking_records_only_the_steps_of_the_plan()
    {
        WelcomeSettings settings = new();

        WelcomeWizardState.MarkShown(
            settings, new WelcomeWizardPlan(IncludeIntro: false, IncludeExamples: true), WizardVersion, RevitVersion);

        Assert.Equal(0, settings.CompletedWizardVersion);
        Assert.Equal(new[] { RevitVersion }, settings.ExamplesOfferedRevitVersions);
    }

    [Fact]
    public void Marking_twice_stores_the_revit_version_once_and_keeps_a_newer_completed_version()
    {
        WelcomeSettings settings = new() { CompletedWizardVersion = 5 };

        WelcomeWizardState.MarkShown(settings, WelcomeWizardPlan.AllSteps, WizardVersion, RevitVersion);
        WelcomeWizardState.MarkShown(settings, WelcomeWizardPlan.AllSteps, WizardVersion, RevitVersion);

        Assert.Equal(5, settings.CompletedWizardVersion);
        Assert.Equal(new[] { RevitVersion }, settings.ExamplesOfferedRevitVersions);
    }
}
