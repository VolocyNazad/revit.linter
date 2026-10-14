using CommunityToolkit.Mvvm.ComponentModel;

namespace Revit.Linter.Presentation.ViewModels;

/// <summary>
/// A predicate-based pane filter with a counted display text, shared by presenter modules.
/// </summary>
/// <typeparam name="TItem">The row type the filter applies to.</typeparam>
/// <typeparam name="TValue">The value identifying the filter within its group.</typeparam>
public partial class FilterViewModel<TItem, TValue> : ObservableObject, IDiagnosticFilter<TItem>
{
    private readonly Func<TItem, bool> _isValid;
    private readonly Func<string> _displayName;

    /// <summary>Initializes the filter with its state, identity, count and matching rules.</summary>
    /// <param name="isActive">Whether the filter initially lets its items through.</param>
    /// <param name="value">The value identifying the filter within its group.</param>
    /// <param name="count">The number of rows matching the filter.</param>
    /// <param name="isValid">Checks whether a row passes the filter.</param>
    /// <param name="displayName">Resolves the display text of the filter.</param>
    /// <remarks>
    /// The display text is resolved on every <see cref="ToString"/> call rather than once
    /// in the constructor, so it follows the current user interface culture.
    /// </remarks>
    public FilterViewModel(
        bool isActive, TValue value, int count, Func<TItem, bool> isValid, Func<string> displayName)
    {
        IsActive = isActive;
        Value = value;
        Count = count;
        _isValid = isValid ?? throw new ArgumentNullException(nameof(isValid));
        _displayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
    }

    /// <summary>Gets or sets whether the filter currently lets its items through.</summary>
    [ObservableProperty]
    public partial bool IsActive { get; set; }

    /// <summary>Gets the value identifying the filter within its group.</summary>
    public TValue Value { get; }

    /// <summary>Gets the number of rows matching the filter.</summary>
    public int Count { get; }

    /// <inheritdoc />
    public bool IsValid(TItem item) => _isValid(item);

    /// <summary>Returns the counted display text of the filter.</summary>
    /// <returns>The count followed by the display text.</returns>
    public override string ToString() => $"{Count} {_displayName()}";
}
