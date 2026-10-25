using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Humanizer;
using Revit.Linter.ReportMessaging;
using System.Globalization;

namespace Revit.Linter.Presentation.ViewModels;

/// <summary>
/// Shared state of a diagnostic or fix report row: the identity, the localized message parts and the
/// command that highlights the report target.
/// </summary>
/// <remarks>
/// Both report presenters build rows from the same message contract, so the row type keeps the parsing,
/// link creation and accent command in one place. A derived row adds only the columns its report shows.
/// </remarks>
public abstract partial class ReportRowViewModel : ObservableObject
{
    /// <summary>Gets the diagnostic code that produced the row.</summary>
    public required string Code { get; init; }

    /// <summary>Gets the title of the document that produced the row.</summary>
    public required string DocumentTitle { get; init; }

    /// <summary>Gets the report message template.</summary>
    public required string Template { get; init; }

    /// <summary>Gets the named values substituted into the message template.</summary>
    public required Dictionary<string, object> Args { get; init; }

    /// <summary>Gets the action that highlights an element of the report target.</summary>
    public required Action<ElementId> AccentElementDelegate { get; init; }

    /// <summary>Gets the time the row was created.</summary>
    public required DateTime Created { get; init; }

    /// <summary>Gets the localized tooltip format of the show-element command.</summary>
    public required string ShowElementToolTipFormat { get; init; }

    /// <summary>Gets the row creation time as relative text in the current user interface culture.</summary>
    public string CreatedText => Created.Humanize(utcDate: false, culture: CultureInfo.CurrentUICulture);

    private ReportMessage? _message;

    private ReportMessage Message => _message ??= ReportMessageParser.Parse(
        Template,
        Args,
        static value => ReportLinks.TryGetLinks<ElementId>(value, static id => id.ToString()));

    /// <summary>Gets the message parts with the element links the message template produced.</summary>
    public IReadOnlyList<ReportTextPart> MessageParts => Message.Parts;

    /// <summary>Gets the plain message text of the row.</summary>
    public string MessageText => Message.Text;

    [RelayCommand]
    private void AccentElement(object? parameter)
    {
        if (parameter is not ElementId elementId) return;
        AccentElementDelegate(elementId);
    }
}
