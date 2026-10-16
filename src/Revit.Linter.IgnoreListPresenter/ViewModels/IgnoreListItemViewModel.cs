using Revit.Linter.Behaviors;

namespace Revit.Linter.IgnoreListPresenter.ViewModels;

/// <summary>
/// One row of the ignore-list pane: a stored ignore entry with display-ready target text.
/// </summary>
internal sealed class IgnoreListItemViewModel : IHasDiagnosticCode
{
    /// <summary>Gets the diagnostic code the entry is ignored for.</summary>
    public required string Code { get; init; }

    /// <summary>Gets the localized scope of the entry: an element or the whole document.</summary>
    public required string ScopeText { get; init; }

    /// <summary>Gets the display name of the ignored target.</summary>
    public required string TargetText { get; init; }

    /// <summary>Gets whether the ignored element no longer exists in the document.</summary>
    public bool IsMissing { get; init; }

    /// <summary>Gets whether the target can be shown in the model right now.</summary>
    public bool HasLiveElement { get; init; }

    /// <summary>Gets the numeric value of the ignored element identifier, if any.</summary>
    public long? ElementIdValue { get; init; }

    /// <summary>Gets the title of the document the entry belongs to.</summary>
    public required string DocumentTitle { get; init; }
}
