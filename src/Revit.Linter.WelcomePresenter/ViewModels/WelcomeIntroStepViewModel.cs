using Revit.Linter.Localization;
using Revit.Linter.WelcomePresenter.Abstractions;

namespace Revit.Linter.WelcomePresenter.ViewModels;

/// <summary>Presents the introduction step that explains what Revit Linter does and where its commands are.</summary>
[GenerateLocalizedProperties]
internal sealed partial class WelcomeIntroStepViewModel(IWelcomeHost host) : WelcomeStepViewModel
{
    public override string Title => TitleText;

    public override string Caption => CaptionText;

    /// <summary>Gets the sentence that names the ribbon tab as the ribbon itself does.</summary>
    public string TabInfo => GetLocalizedString("tab_format", host.RibbonTabName);
}
