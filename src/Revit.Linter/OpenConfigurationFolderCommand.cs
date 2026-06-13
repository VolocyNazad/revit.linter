using Autodesk.Revit.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Revit.Linter.ConfigurationPath;
using Revit.Linter.Infrastructure.ExternalCommands;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Revit.Linter;

[Transaction(TransactionMode.Manual)]
public class OpenConfigurationFolderCommand : ExternalCommand
{
    private IServiceProvider Provider => Program.Provider;
    private ILogger Logger => field 
        ??= Provider.GetRequiredService<ILogger<OpenConfigurationFolderCommand>>();

    public override void Execute()
    {
        try
        {
            string directory = ConfigurationPathUtils.Directory;
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            Process.Start("explorer.exe", directory);
        }
        catch (Exception ex)
        {
            string message = "Open configuration folder failed.";
            Logger.LogError(ex, message);
            MessageBox.Show(message); // todo use custom dialog
        }
    }
}
