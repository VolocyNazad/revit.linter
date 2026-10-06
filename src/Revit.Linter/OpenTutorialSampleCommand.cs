using Autodesk.Revit.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Revit.Linter.Infrastructure.ExternalCommands;
using Revit.Linter.Infrastructure.Services;
using Revit.Linter.WelcomePresenter.Abstractions;
using Revit.Linter.WelcomePresenter.Abstractions.Services;
using Revit.Linter.ConfigurationPath;
using System.IO;

namespace Revit.Linter;

/// <summary>Opens the disposable Autodesk sample prepared by the welcome experience.</summary>
/// <remarks>The command consumes a one-time request and never opens or modifies the original Autodesk sample.</remarks>
[Transaction(TransactionMode.Manual)]
public sealed class OpenTutorialSampleCommand : ExternalCommand
{
    /// <inheritdoc />
    public override void Execute()
    {
        IServiceProvider provider = Program.Provider;
        TutorialSampleOpenRequest request = provider.GetRequiredService<TutorialSampleOpenRequest>();
        if (!request.TryTake(out string? path) || path is null)
        {
            InitExternalApplication.SetTutorialSampleButtonVisible(false);
            return;
        }

        try
        {
            Application.OpenAndActivateDocument(path);
            request.TrackOpened(path);
            provider.GetRequiredService<IWelcomeHost>().ShowPanes();
            provider.GetRequiredService<IPracticalTour>().Start(hasOpenDocument: true, restart: true);
            provider.GetRequiredService<ILogger<OpenTutorialSampleCommand>>()
                .LogInformation("Opened tutorial sample copy {TutorialSamplePath}", path);
            InitExternalApplication.SetTutorialSampleButtonVisible(false);
        }
        catch (Exception exception) when (exception is IOException or Autodesk.Revit.Exceptions.ApplicationException)
        {
            provider.GetRequiredService<ILogger<OpenTutorialSampleCommand>>()
                .LogError(exception, "Failed to open tutorial sample copy {TutorialSamplePath}", path);
            provider.GetRequiredService<ITutorialSampleCopyService>()
                .DeleteCopy(path, ConfigurationPathUtils.RevitVersion);
            InitExternalApplication.SetTutorialSampleButtonVisible(false);
        }
    }
}
