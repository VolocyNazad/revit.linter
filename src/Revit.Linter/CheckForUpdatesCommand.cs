using Autodesk.Revit.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Revit.Linter.DialogPresenter.Abstractions;
using Revit.Linter.Infrastructure.ExternalCommands;
using Revit.Linter.Infrastructure.Services;
using System.ComponentModel;
using System.IO;

namespace Revit.Linter;

/// <summary>Starts the standalone Revit Linter updater for an explicit user check.</summary>
/// <remarks>
/// The add-in performs no network request. The updater executable owns release discovery and user
/// feedback, so a failed update check cannot affect Revit startup or the active document.
/// </remarks>
[Transaction(TransactionMode.Manual)]
public sealed class CheckForUpdatesCommand : ExternalCommand
{
    /// <inheritdoc />
    public override void Execute()
    {
        try
        {
            UpdaterProcessRunner.Start("--check-now");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or Win32Exception)
        {
            IServiceProvider provider = Program.Provider;
            provider.GetRequiredService<ILogger<CheckForUpdatesCommand>>()
                .LogError(exception, "Failed to start the standalone updater");
            var localizer = provider.GetRequiredService<IStringLocalizer<GlobalLocalizations>>();
            _ = provider.GetRequiredService<IDialog>().Show(new DialogRequest(
                localizer["updater_startFailed_message", exception.Message]));
        }
    }
}
