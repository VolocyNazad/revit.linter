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

    /// <summary>Gets or sets the one-based position of the step among the shown steps.</summary>
    [ObservableProperty]
    public partial int Number { get; set; }

    /// <summary>Gets or sets a value indicating whether the wizard currently displays this step.</summary>
    [ObservableProperty]
    public partial bool IsCurrent { get; set; }
}
