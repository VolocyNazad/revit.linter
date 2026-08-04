using Autodesk.Revit.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using Revit.Linter.ConfigurationPath;
using Revit.Linter.DialogPresenter.Abstractions;
using Revit.Linter.Infrastructure.ExternalCommands;
using System.Diagnostics;
using System.IO;

namespace Revit.Linter;

[Transaction(TransactionMode.Manual)]
public class OpenConfigurationFolderCommand : ExternalCommand
{
    private static IServiceProvider Provider => Program.Provider;
    private ILogger Logger => field 
        ??= Provider.GetRequiredService<ILogger<OpenConfigurationFolderCommand>>();
    private IDialog Dialog => field ??= Provider.GetRequiredService<IDialog>();
    private IStringLocalizer<GlobalLocalizations> Localizer => field
        ??= Provider.GetRequiredService<IStringLocalizer<GlobalLocalizations>>();

    public override void Execute()
    {
        try
        {
            string directory = ConfigurationPathUtils.Directory;
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            Process.Start(new ProcessStartInfo
            {
                FileName = directory,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            string message = Localizer["configurationFolder_openFailed_message", ex.Message];
            Logger.LogError(ex, message);
            _ = Dialog.Show(new DialogRequest(message));
        }
    }
}
