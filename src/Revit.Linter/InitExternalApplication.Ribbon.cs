// Ribbon tab and product buttons.

using Revit.Linter.Core.Abstractions.Models;
#if !AFTER2024
using Revit.Sugar;
#endif

namespace Revit.Linter;

internal sealed partial class InitExternalApplication
{
    private static void AddRibbonButtons(RibbonPanel panel)
    {
        panel.AddItem(CreateButton(
            "ShowAllPanesButton", "showAllPanes", typeof(ShowAllPanesCommand),
            new("Dockable panes", "Закрепляемые панели")));
        panel.AddStackedItems(
            CreateButton(
                "CheckForUpdatesButton", "checkForUpdates", typeof(CheckForUpdatesCommand),
                new("Check for updates button", "Кнопка проверки обновлений"), compact: true),
            CreateButton(
                "OpenSupportButton", "support", typeof(OpenSupportCommand),
                new("Support button", "Кнопка поддержки"), compact: true),
            CreateButton(
                "OpenSponsorButton", "sponsor", typeof(OpenSponsorCommand),
                new("Sponsor button", "Кнопка спонсорства"), compact: true));
        panel.AddItem(CreateButton(
            "ShowWelcomeButton", "showWelcome", typeof(ShowWelcomeCommand),
            new("Getting started button", "Кнопка начала работы")));
        panel.AddItem(CreateButton(
            "ShowPracticalTourButton", "showPracticalTour", typeof(ShowPracticalTourCommand),
            new("Practical tour", "Практическое обучение")));
        _tutorialSampleButton = (PushButton)panel.AddItem(CreateButton(
            "OpenTutorialSampleButton", "openTutorialSample", typeof(OpenTutorialSampleCommand),
            new("Getting started button", "Кнопка начала работы")));
        _tutorialSampleButton.Visible = false;
    }

    internal static void SetTutorialSampleButtonVisible(bool visible)
    {
        if (_tutorialSampleButton is not null) _tutorialSampleButton.Visible = visible;
    }

    /// <summary>
    /// Creates a ribbon button whose texts are the <c>_buttonText</c>, <c>_toolTip</c> and
    /// <c>_longDescription</c> resources that start with <paramref name="resourcePrefix"/>.
    /// </summary>
    /// <remarks>A compact button is meant for a stacked column: it has a 16-pixel image and no large one.</remarks>
    private static PushButtonData CreateButton(
        string name, string resourcePrefix, Type commandType, DocumentationPage helpPage, bool compact = false)
    {
        PushButtonData buttonData = new(
            name, Localizer[$"{resourcePrefix}_buttonText"], AssemblyPath, commandType.FullName)
        {
            ToolTip = Localizer[$"{resourcePrefix}_toolTip"],
            LongDescription = Localizer[$"{resourcePrefix}_longDescription"],
            Image = compact ? SmallIcon : Icon,
            ToolTipImage = Icon
        };
        if (!compact) buttonData.LargeImage = Icon;

        // F1 over the button opens its documentation page in the language of the current UI culture.
        buttonData.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, helpPage.GetUrl()));
        return buttonData;
    }
}

