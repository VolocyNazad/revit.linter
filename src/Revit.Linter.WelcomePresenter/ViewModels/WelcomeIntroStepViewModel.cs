using Revit.Linter.ConfigurationPath;
using Revit.Linter.Localization;
using Revit.Linter.WelcomePresenter.Abstractions;

namespace Revit.Linter.WelcomePresenter.ViewModels;

[GenerateLocalizedProperties]
internal sealed partial class WelcomeIntroStepViewModel : WelcomeStepViewModel
{
    private readonly IWelcomeHost _host;

    public WelcomeIntroStepViewModel(IWelcomeHost host) => _host = host;

    public override string Title => TitleText;

    public override string Caption => CaptionText;

    /// <summary>Gets the sentence that names the ribbon tab as the ribbon itself does.</summary>
    public string TabInfo => GetLocalizedString("tab_format", _host.RibbonTabName);

    public string ConfigurationInfo => GetLocalizedString("configuration_format", ConfigurationPathUtils.Directory);

    public string LogsInfo => GetLocalizedString("logs_format", _host.LogDirectory);
}
