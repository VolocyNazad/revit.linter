#pragma warning disable S2325 // The XAML designer binds to instance members only, so design properties stay non-static.

using Revit.Linter.Localization;

namespace Revit.Linter.DialogPresenter.DesignData;

/// <summary>Provides representative sample values for <see cref="Views.DialogView"/> in the XAML designer.</summary>
internal sealed class DialogDesignData
{
    private const string ResourceBaseName = "Revit.Linter.Localization.DialogPresenter.ViewModels.DialogViewModel";

    /// <summary>Gets the sample dialog body, rendered through the string template of the view.</summary>
    public object Content => "The diagnostic configuration was updated successfully.";

    /// <summary>Gets the localized close button text.</summary>
    public string CloseButtonText => LocalizationResourceReader.GetString(ResourceBaseName, "closeButton_text");
}
