using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Humanizer;
using Revit.Linter.ReportMessaging;
using System.Globalization;

namespace Revit.Linter.FixReportPresenter.ViewModels;

internal sealed partial class FixReportItemViewModel : ObservableObject
{
    public required string Code { get; init; }
    public required string DocumentTitle { get; init; }
    public required string Template { get; init; }
    public required Dictionary<string, object> Args { get; init; }
    public required Action<ElementId> AccentElementDelegate { get; init; }
    public required DateTime Created { get; init; }
    public string CreatedText => Created.Humanize(utcDate: false, culture: CultureInfo.CurrentUICulture);
    public required string ShowElementToolTipFormat { get; init; }

    private ReportMessage? _message;
    private ReportMessage Message => _message ??= ReportMessageParser.Parse(
        Template,
        Args,
        static value => ReportLinks.TryGetLinks<ElementId>(value, static id => id.ToString()));

    public IReadOnlyList<ReportTextPart> MessageParts => Message.Parts;
    public string MessageText => Message.Text;

    [RelayCommand]
    private void AccentElement(object? parameter)
    {
        if (parameter is not ElementId elementId) return;
        AccentElementDelegate(elementId);
    }
}
