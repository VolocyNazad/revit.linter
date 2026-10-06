using CommunityToolkit.Mvvm.Input;
using Revit.Linter.Localization;
using Revit.Linter.WelcomePresenter.Abstractions;

namespace Revit.Linter.WelcomePresenter.ViewModels;

/// <summary>Summarizes onboarding and provides documentation links.</summary>
[GenerateLocalizedProperties]
internal sealed partial class WelcomeFinishStepViewModel(IWelcomeHost host) : WelcomeStepViewModel
{
    public override string Title => TitleText;

    public override string Caption => CaptionText;

    public string ReopenInfo => GetLocalizedString("reopen_format", host.RibbonTabName);

    [RelayCommand]
    private void OpenQuickStart() => host.OpenQuickStart();

    [RelayCommand]
    private void OpenDocumentation() => host.OpenDocumentation();

    [RelayCommand]
    private void OpenSupport() => host.OpenSupport();
}
