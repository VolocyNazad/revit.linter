using CommunityToolkit.Mvvm.ComponentModel;
using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.Presentation.ViewModels;

/// <summary>
/// The two switches of a search box: match case and match whole word.
/// </summary>
/// <remarks>
/// The switches belong to a pane view model: the pane shows them next to its search box and refreshes
/// the list when <c>changed</c> runs. Both start off, so the default search ignores case and matches
/// part of a word; a pane that restores a saved search sets them after creation.
/// </remarks>
public sealed partial class SearchOptionsViewModel : ObservableObject
{
    private readonly Action? _changed;

    /// <summary>Initializes the switches in their default state.</summary>
    /// <param name="changed">Runs after a switch changes, so the owner can refresh its list.</param>
    public SearchOptionsViewModel(Action? changed = null) => _changed = changed;

    /// <summary>Gets or sets whether the row text must have the same letter case as the search text.</summary>
    [ObservableProperty]
    public partial bool MatchCase { get; set; }
    partial void OnMatchCaseChanged(bool value) => _changed?.Invoke();

    /// <summary>Gets or sets whether the search text must match whole words.</summary>
    [ObservableProperty]
    public partial bool MatchWholeWord { get; set; }
    partial void OnMatchWholeWordChanged(bool value) => _changed?.Invoke();

    /// <summary>Returns the switches as the options a search query compares text with.</summary>
    /// <returns>The current options of the search box.</returns>
    public SearchMatchOptions ToMatchOptions() => new(MatchCase, MatchWholeWord);
}
