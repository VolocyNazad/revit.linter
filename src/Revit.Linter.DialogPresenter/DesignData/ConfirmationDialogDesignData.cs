#pragma warning disable S2325 // The XAML designer binds to instance members only, so design properties stay non-static.

using CommunityToolkit.Mvvm.Input;
using Revit.Linter.Localization;
using System.Windows.Input;

namespace Revit.Linter.DialogPresenter.DesignData;

/// <summary>Provides representative sample values for <see cref="Views.ConfirmationDialogView"/> in the XAML designer.</summary>
internal sealed class ConfirmationDialogDesignData
{
    private const string ResourceBaseName = "Revit.Linter.Localization.DialogPresenter.ViewModels.ConfirmationDialogViewModel";

    /// <summary>Gets the sample confirmation question, rendered through the string template of the view.</summary>
    public object Content => "Apply the selected fixes to 12 elements? This operation changes the current document.";

    /// <summary>Gets the localized close button text.</summary>
    public string CloseButtonText => LocalizationResourceReader.GetString(ResourceBaseName, "closeButton_text");

    /// <summary>Gets the confirmation button text; the view model receives it from the caller.</summary>
    public string ConfirmButtonText => "Apply";

    /// <summary>Gets the dialog result the window behavior applies; no preset result is shown.</summary>
    public bool? DialogResult => null;

    /// <summary>Gets the command that closes the dialog with the requested result.</summary>
    public ICommand CloseWithResultCommand { get; } = new RelayCommand<bool>(_ => { });
}
