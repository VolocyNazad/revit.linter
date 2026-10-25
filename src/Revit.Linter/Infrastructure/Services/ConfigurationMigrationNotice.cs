using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Revit.Linter.ConfigurationPath.Abstractions.Services;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.DialogPresenter.Abstractions;
using Revit.Linter.Infrastructure.Models;
using Toolkit.ValueStore.Abstractions;
using System.IO;

namespace Revit.Linter.Infrastructure.Services;

/// <summary>
/// Tells the user once per Revit version that the old separate configuration files are no longer read.
/// </summary>
/// <remarks>
/// The notice is shown only when a legacy file actually exists, so a clean installation never sees it.
/// The version is remembered before the dialog opens, so a refused or closed dialog does not repeat on
/// every start. The old files are never modified or deleted by this service.
/// </remarks>
internal sealed class ConfigurationMigrationNotice(
    IValueStore<ConfigurationMigrationNoticeSettings> settingsStore,
    IConfirmationDialog confirmationDialog,
    IDocumentationLauncher documentationLauncher,
    IStringLocalizer<GlobalLocalizations> localizer,
    IConfigurationPaths paths,
    ILogger<ConfigurationMigrationNotice> logger)
{
    private static readonly DocumentationPage MigrationPage =
        new("Migrating configuration", "Миграция конфигурации");

    /// <summary>Shows the migration notice when legacy files exist and it was not shown for this Revit version yet.</summary>
    /// <param name="cancellationToken">A token that cancels the display operation.</param>
    public async Task ShowIfNeededAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!paths.LegacyConfigurationPaths.Any(File.Exists)) return;

            int revitVersion = paths.RevitVersion;
            if (settingsStore.CurrentValue.NotifiedRevitVersions.Contains(revitVersion)) return;

            settingsStore.Update(settings =>
            {
                if (!settings.NotifiedRevitVersions.Contains(revitVersion))
                    settings.NotifiedRevitVersions.Add(revitVersion);
            });
            logger.LogInformation(
                "Showing the configuration migration notice for Revit {RevitVersion}", revitVersion);

            bool openGuide = await confirmationDialog.Show(
                new ConfirmationDialogRequest(
                    localizer["configurationMigration_notice_message"],
                    localizer["configurationMigration_notice_openGuide"]),
                cancellationToken);
            if (openGuide)
                documentationLauncher.Open(MigrationPage);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to show the configuration migration notice");
        }
    }
}
