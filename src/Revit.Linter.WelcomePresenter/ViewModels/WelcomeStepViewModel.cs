using CommunityToolkit.Mvvm.ComponentModel;

namespace Revit.Linter.WelcomePresenter.ViewModels;

/// <summary>
/// The state shared by every step of the welcome wizard.
/// </summary>
internal abstract partial class WelcomeStepViewModel : ObservableObject
{
    /// <summary>Gets the short step name shown in the step list.</summary>
    public abstract string Title { get; }

    /// <summary>Gets the light-hearted caption shown above the step content.</summary>
    public abstract string Caption { get; }

    /// <summary>Gets a value indicating whether the step shows a caption above its content.</summary>
    public bool HasCaption => Caption.Length > 0;

    /// <summary>Gets the one-line description shown in the contents of the welcome window.</summary>
    public abstract string Summary { get; }

    /// <summary>Gets a value indicating whether the window footer shows the action of the step.</summary>
    public virtual bool HasPrimaryAction => false;

    /// <summary>Gets the label of the window footer action; it is empty when the step has none.</summary>
    public virtual string PrimaryActionText => string.Empty;

    /// <summary>Gets the tooltip of the window footer action; it is empty when the step has none.</summary>
    public virtual string? PrimaryActionToolTip => null;
}
