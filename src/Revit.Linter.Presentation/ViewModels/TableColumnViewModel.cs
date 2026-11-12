using CommunityToolkit.Mvvm.ComponentModel;

namespace Revit.Linter.Presentation.ViewModels;

/// <summary>
/// One checkable column of a table visibility popup.
/// </summary>
public sealed class TableColumnViewModel : ObservableObject
{
    private readonly Action<TableColumnViewModel, bool> _visibilityChanged;
    private readonly Func<string> _title;
    private bool _isVisible;

    /// <summary>Initializes the column with its key, title and current visibility.</summary>
    /// <param name="key">The stored column key, one of <see cref="Models.TableColumnKeys"/>.</param>
    /// <param name="title">Resolves the localized column title.</param>
    /// <param name="isVisible">Whether the column is currently visible.</param>
    /// <param name="visibilityChanged">Receives a visibility change made by the user.</param>
    public TableColumnViewModel(
        string key,
        Func<string> title,
        bool isVisible,
        Action<TableColumnViewModel, bool> visibilityChanged)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        _title = title ?? throw new ArgumentNullException(nameof(title));
        _visibilityChanged = visibilityChanged ?? throw new ArgumentNullException(nameof(visibilityChanged));
        _isVisible = isVisible;
    }

    /// <summary>Gets the stored column key.</summary>
    public string Key { get; }

    /// <summary>Gets the localized title shown next to the checkbox.</summary>
    public string Title => _title();

    /// <summary>Gets or sets whether the table shows the column.</summary>
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible == value) return;
            _isVisible = value;
            OnPropertyChanged();
            _visibilityChanged(this, value);
        }
    }

    /// <summary>Applies a visibility chosen outside the popup without reporting it back as a user action.</summary>
    /// <param name="isVisible">Whether the table shows the column now.</param>
    internal void ApplyVisibility(bool isVisible) => SetProperty(ref _isVisible, isVisible, nameof(IsVisible));
}
