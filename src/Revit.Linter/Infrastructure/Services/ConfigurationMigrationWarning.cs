using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Revit.Linter.ConfigurationPath.Abstractions.Models;
using Revit.Linter.ConfigurationPath.Abstractions.Services;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.DialogPresenter.Abstractions;
using Revit.Linter.Infrastructure.Models;
using Toolkit.ValueStore.Abstractions;

namespace Revit.Linter.Infrastructure.Services;

/// <summary>
/// Moves the legacy split configuration files of every Revit version once, then tells the user once per product
/// version that those rules are no longer read.
/// </summary>
/// <remarks>
/// The migration runs during startup before anything reads configuration, so the dialog only reports an action
/// that already happened. It is shown once per product version: after the files were moved there is nothing left
/// to report, and a file that could not be moved stays inert in place. The old files are never deleted, so the
/// legacy folder remains the way back to an older product version. A failure is logged and never affects startup.
/// </remarks>
internal sealed class ConfigurationMigrationWarning(
    IValueStore<ConfigurationMigrationWarningSettings> settingsStore,
    IConfirmationDialog confirmationDialog,
    IDocumentationLauncher documentationLauncher,
    IStringLocalizer<GlobalLocalizations> localizer,
    IConfigurationMigrationService migrationService,
    ILogger<ConfigurationMigrationWarning> logger)
{
    private static readonly DocumentationPage MigrationPage =
        new("Migrating configuration", "Миграция конфигурации");

    private ConfigurationMigrationResult _result = ConfigurationMigrationResult.None;

    /// <summary>Moves the legacy configuration files of every Revit version and remembers what happened.</summary>
    /// <remarks>
    /// Call it once during startup, before anything reads the diagnostic configuration. A migration failure is
    /// logged and never prevents the add-in from starting: the old files are not read either way, and the user
    /// keeps seeing the migration guide in the Wiki.
    /// </remarks>
    public void Migrate()
    {
        try
        {
            _result = migrationService.Migrate();
            if (_result.HasFindings)
                settingsStore.Update(settings => settings.PendingWarning = true);
        }
        catch (Exception exception)
        {
            _result = ConfigurationMigrationResult.None;
            logger.LogError(exception, "Failed to move the legacy configuration files into the legacy folders");
        }
    }

    /// <summary>Shows the warning when legacy files were found and this product version has not warned yet.</summary>
    /// <param name="productVersion">The informational version of the running product.</param>
    /// <param name="cancellationToken">A token that cancels the display operation.</param>
    public async Task ShowIfNeededAsync(string productVersion, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_result.HasFindings && !settingsStore.CurrentValue.PendingWarning) return;
            if (string.Equals(settingsStore.CurrentValue.NotifiedVersion, productVersion, StringComparison.Ordinal))
            {
                // Defensive: a previous run recorded the version but could not clear the pending flag.
                if (settingsStore.CurrentValue.PendingWarning)
                    settingsStore.Update(settings => settings.PendingWarning = false);
                return;
            }

            logger.LogInformation(
                "Showing the configuration migration warning for product version {ProductVersion}; {MovedCount} files were moved, {RemainingCount} remain, pending from an earlier start: {Pending}",
                productVersion,
                _result.MovedFiles.Count,
                _result.RemainingFiles.Count,
                !_result.HasFindings);

            string message = _result.RemainingFiles.Count == 0
                ? localizer["configurationMigration_warning_message"]
                : localizer["configurationMigration_warning_message"]
                  + Environment.NewLine + Environment.NewLine
                  + localizer["configurationMigration_warning_remaining_message"];

            bool openGuide = await confirmationDialog.Show(
                new ConfirmationDialogRequest(message, localizer["configurationMigration_warning_openGuide"]),
                cancellationToken);

            settingsStore.Update(settings =>
            {
                settings.NotifiedVersion = productVersion;
                settings.PendingWarning = false;
            });

            if (openGuide)
                documentationLauncher.Open(MigrationPage);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to show the configuration migration warning");
        }
    }
}
