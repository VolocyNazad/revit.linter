using Revit.Linter.ConfigurationPath;
using Revit.Linter.Localization;

namespace Revit.Linter.WelcomePresenter.ViewModels;

/// <summary>
/// Explains where diagnostics come from without coupling the welcome flow to diagnostic implementations.
/// </summary>
[GenerateLocalizedProperties]
internal sealed partial class WelcomeRulesStepViewModel : WelcomeStepViewModel
{
    private const int FirstRevitVersionWithGroupTypeIds = 2024;

    public override string Title => TitleText;

    public override string Caption => CaptionText;

    /// <summary>Gets a copy-ready project parameter rule for the running Revit version.</summary>
    public string ProjectParameterRulePreviewText
    {
        get
        {
            string parameterGroup = ConfigurationPathUtils.RevitVersion < FirstRevitVersionWithGroupTypeIds
                ? "PG_DATA"
                : "autodesk.parameter.group:data-1.0.0";
            return ProjectParameterRuleTemplateText.Replace("{parameterGroup}", parameterGroup);
        }
    }
}
