using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    private static readonly Func<object?, IReadOnlyList<ReportLink>?> LinkProvider =
        static value => ReportLinks.TryGetLinks<ElementId>(value, static id => id.ToString());

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

    /// <summary>Gets the localized tooltip of the control that reveals a folded list.</summary>
    public required string ExpandListToolTip { get; init; }

    /// <summary>Gets the row creation time as relative text in the current user interface culture.</summary>
    /// <remarks>
    /// The text is measured against the current time on every read, so it only stays correct while the
    /// bound cell re-reads it. <see cref="RefreshCreatedText"/> is what makes that happen for an open pane.
    /// </remarks>
    public string CreatedText => ReportRowAge.Format(Created, DateTime.Now, CultureInfo.CurrentUICulture);

    /// <summary>Re-raises <see cref="CreatedText"/> so a bound cell re-reads the age against the current time.</summary>
    public void RefreshCreatedText() => OnPropertyChanged(nameof(CreatedText));

    private ReportMessageDisplay? _display;
    private IReadOnlyList<ReportTextPart>? _expandedListParts;

    private ReportMessageDisplay Display => _display ??= new ReportMessageDisplay(
        Template,
        Args,
        LinkProvider,
        ReportDisplayBudget.Default);

    /// <summary>Gets the message parts with the element links the message template produced.</summary>
    /// <remarks>
    /// A list value with many elements or names is folded here, so the row of the report table stays
    /// readable. <see cref="MessageText"/> keeps the complete message: folding only changes what the
    /// table renders inline, and the expander of the list carries the complete list.
    /// </remarks>
    public IReadOnlyList<ReportTextPart> MessageParts => Display.Parts;

    /// <summary>Gets the complete parts of the list the expander opened, or <see langword="null"/> until one is opened.</summary>
    public IReadOnlyList<ReportTextPart>? ExpandedListParts => _expandedListParts;

    /// <summary>Gets the plain message text of the row.</summary>
    public string MessageText => Display.Text;

    /// <summary>Gets or sets whether the expanded list is shown in the popup of the row.</summary>
    /// <remarks>
    /// A popup that closes on its own (a click outside it) does not write that state back through a
    /// two-way binding, so the view reports the close through <see cref="CollapseListCommand"/>.
    /// </remarks>
    [ObservableProperty]
    private bool _isListExpanded;

    /// <summary>Gets the command that opens a folded list with all of its items.</summary>
    /// <param name="parameter">The complete parts of the list, carried by the expander part.</param>
    /// <remarks>
    /// The state is set to closed first so a popup that is already open closes and opens again for the
    /// newly clicked list; a popup that closed by itself never reported that through the two-way binding.
    /// </remarks>
    [RelayCommand]
    private void ExpandList(object? parameter)
    {
        if (parameter is not IReadOnlyList<ReportTextPart> parts) return;

        _expandedListParts = parts;
        OnPropertyChanged(nameof(ExpandedListParts));
        IsListExpanded = false;
        IsListExpanded = true;
    }

    /// <summary>Gets the command that marks the expanded list popup as closed.</summary>
    /// <remarks>
    /// Called when the popup closes by itself, so the next expander click opens it again instead of
    /// setting a state the popup already has.
    /// </remarks>
    [RelayCommand]
    private void CollapseList() => IsListExpanded = false;

    [RelayCommand]
    private void AccentElement(object? parameter)
    {
        if (parameter is not ElementId elementId) return;
        AccentElementDelegate(elementId);
    }
}
