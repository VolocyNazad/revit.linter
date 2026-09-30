using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Revit.Linter.DialogPresenter.Abstractions;
using System.Diagnostics;

namespace Revit.Linter.Infrastructure.Utils;

internal static class ExternalPageLauncher
{
    public static void Open<TCommand>(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception exception)
        {
            IServiceProvider provider = Program.Provider;
            provider.GetRequiredService<ILogger<TCommand>>()
                .LogError(exception, "Failed to open external page {Url}", url);

            var localizer = provider.GetRequiredService<IStringLocalizer<GlobalLocalizations>>();
            _ = provider.GetRequiredService<IDialog>().Show(new DialogRequest(
                localizer["externalPage_openFailed_message", exception.Message]));
        }
    }
}
