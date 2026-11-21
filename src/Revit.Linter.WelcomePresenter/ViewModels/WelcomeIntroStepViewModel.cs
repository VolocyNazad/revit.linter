using CommunityToolkit.Mvvm.Input;
using Revit.Linter.Localization;
using Revit.Linter.WelcomePresenter.Abstractions;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Revit.Linter.WelcomePresenter.ViewModels;

/// <summary>Presents the main page: what Revit Linter does, a contents list of the other steps and links.</summary>
[GenerateLocalizedProperties]
internal sealed partial class WelcomeIntroStepViewModel(IWelcomeHost host) : WelcomeStepViewModel
{
    private const string StepGlyph = "\u2192";
    private const string ExternalPageGlyph = "\u2197";

    public override string Title => TitleText;

    public override string Caption => string.Empty;

    public override string Summary => SummaryText;

    /// <summary>Gets the rows listed in the contents of the main page.</summary>
    public ObservableCollection<WelcomeContentsEntryViewModel> Contents { get; } = [];

    /// <summary>Fills the contents with the steps the wizard shows after this one and the help links.</summary>
    internal void SetContents(IEnumerable<WelcomeStepViewModel> steps, ICommand goToStepCommand)
    {
        Contents.Clear();
        foreach (WelcomeStepViewModel step in steps)
        {
            Contents.Add(new WelcomeContentsEntryViewModel
            {
                Title = step.Title,
                Summary = step.Summary,
                Command = goToStepCommand,
                CommandParameter = step,
                ActionGlyph = StepGlyph,
            });
        }

        Contents.Add(new WelcomeContentsEntryViewModel
        {
            Title = QuickStartText,
            Summary = QuickStartSummaryText,
            Command = OpenQuickStartCommand,
            ActionGlyph = ExternalPageGlyph,
        });
        Contents.Add(new WelcomeContentsEntryViewModel
        {
            Title = DocumentationText,
            Summary = DocumentationSummaryText,
            Command = OpenDocumentationCommand,
            ActionGlyph = ExternalPageGlyph,
        });
        Contents.Add(new WelcomeContentsEntryViewModel
        {
            Title = SupportText,
            Summary = SupportSummaryText,
            Command = OpenSupportCommand,
            ActionGlyph = ExternalPageGlyph,
        });
    }

    [RelayCommand]
    private void OpenQuickStart() => host.OpenQuickStart();

    [RelayCommand]
    private void OpenDocumentation() => host.OpenDocumentation();

    [RelayCommand]
    private void OpenSupport() => host.OpenSupport();
}
