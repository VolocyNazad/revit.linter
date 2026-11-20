using System.Windows.Input;

namespace Revit.Linter.WelcomePresenter.ViewModels;

/// <summary>Describes one row of the main-page contents: a wizard step or an external page.</summary>
internal sealed class WelcomeContentsEntryViewModel
{
    /// <summary>Gets the row title.</summary>
    public required string Title { get; init; }

    /// <summary>Gets the one-line description shown under the title.</summary>
    public required string Summary { get; init; }

    /// <summary>Gets the command the row runs.</summary>
    public required ICommand Command { get; init; }

    /// <summary>Gets the command parameter of a step row.</summary>
    public object? CommandParameter { get; init; }

    /// <summary>Gets the glyph that shows where the row leads.</summary>
    public required string ActionGlyph { get; init; }
}
