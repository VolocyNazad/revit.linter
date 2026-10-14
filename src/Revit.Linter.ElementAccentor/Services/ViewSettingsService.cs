using Microsoft.Extensions.Logging;
using Revit.Linter.ElementAccentor.Abstractions.Services;

namespace Revit.Linter.ElementAccentor.Services;

internal sealed class ViewSettingsService(ILogger<ViewSettingsService> logger) : IViewSettingsService
{
    private readonly ILogger<ViewSettingsService> _logger = logger;

    public IRestoreSession ApplyDetailLevel(Document document, View view, ViewDetailLevel level)
    {
        ViewDetailLevel previousLevel = view.DetailLevel;
        if (previousLevel == level)
            return new RestoreSession(static () => { });

        TransactionExecutor.Execute(document, ElementAccentorLocalizations.GetString("applyDetailLevel_transaction"), () => view.DetailLevel = level);
        _logger.LogInformation(
            "View {ViewName} detail level set to {DetailLevel}.", view.Name, level);

        return new RestoreSession(() =>
        {
            if (!document.IsValidObject || !view.IsValidObject) return;
            TransactionExecutor.Execute(document, ElementAccentorLocalizations.GetString("restoreDetailLevel_transaction"), () =>
            {
                if (view.DetailLevel != previousLevel)
                    view.DetailLevel = previousLevel;
            });
        });
    }

    public IRestoreSession ApplyDisplayStyle(Document document, View view, DisplayStyle style)
    {
        DisplayStyle previousStyle = view.DisplayStyle;
        if (previousStyle == style)
            return new RestoreSession(static () => { });

        TransactionExecutor.Execute(document, ElementAccentorLocalizations.GetString("applyDisplayStyle_transaction"), () => view.DisplayStyle = style);
        _logger.LogInformation(
            "View {ViewName} display style set to {DisplayStyle}.", view.Name, style);

        return new RestoreSession(() =>
        {
            if (!document.IsValidObject || !view.IsValidObject) return;
            TransactionExecutor.Execute(document, ElementAccentorLocalizations.GetString("restoreDisplayStyle_transaction"), () =>
            {
                if (view.DisplayStyle != previousStyle)
                    view.DisplayStyle = previousStyle;
            });
        });
    }
}
