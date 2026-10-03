using Autodesk.Revit.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Revit.Linter.DialogPresenter.Abstractions;
using Revit.Linter.Infrastructure.ExternalCommands;
using Revit.Linter.WelcomePresenter.Abstractions;

namespace Revit.Linter;

/// <summary>Opens the welcome wizard with every step, regardless of whether it was shown before.</summary>
/// <remarks>The command does not require an active document or a Revit transaction.</remarks>
[Transaction(TransactionMode.Manual)]
public sealed class ShowWelcomeCommand : ExternalCommand
{
    /// <inheritdoc />
    public override void Execute()
    {
        IServiceProvider provider = Program.Provider;
        try
        {
            provider.GetRequiredService<IWelcomeWizard>().Show();
        }
        catch (Exception exception)
        {
            provider.GetRequiredService<ILogger<ShowWelcomeCommand>>()
                .LogError(exception, "Failed to show the welcome wizard");
            var localizer = provider.GetRequiredService<IStringLocalizer<GlobalLocalizations>>();
            _ = provider.GetRequiredService<IDialog>().Show(new DialogRequest(
                localizer["welcome_showFailed_message", exception.Message]));
        }
    }
}
