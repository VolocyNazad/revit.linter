using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Revit.Linter.Localization;
using Revit.Linter.WelcomePresenter.Abstractions;

namespace Revit.Linter.WelcomePresenter.ViewModels;

[GenerateLocalizedProperties]
internal sealed partial class WelcomeFinishStepViewModel : WelcomeStepViewModel
{
    private readonly IWelcomeHost _host;

    public WelcomeFinishStepViewModel(IWelcomeHost host)
    {
        _host = host;
        OpenPanes = true;
    }

    public override string Title => TitleText;

    public override string Caption => CaptionText;

    public string ReopenInfo => GetLocalizedString("reopen_format", _host.RibbonTabName);

    /// <summary>Gets or sets a value indicating whether the panes are shown after the wizard is finished.</summary>
    [ObservableProperty]
    public partial bool OpenPanes { get; set; }

    /// <summary>Gets or sets an optional remark about the previous steps; <see langword="null"/> hides it.</summary>
    [ObservableProperty]
    public partial string? Note { get; set; }

    [RelayCommand]
    private void OpenQuickStart() => _host.OpenQuickStart();

    [RelayCommand]
    private void OpenDocumentation() => _host.OpenDocumentation();

    [RelayCommand]
    private void OpenSupport() => _host.OpenSupport();
}
