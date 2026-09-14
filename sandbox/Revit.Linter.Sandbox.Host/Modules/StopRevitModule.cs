using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ModularPipelines.Context;
using ModularPipelines.Modules;

namespace Revit.Linter.Sandbox.Host.Modules;

public sealed class StopRevitModule : Module<object?>
{
    private static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(10);

    protected override async Task<object?> ExecuteAsync(
        IModuleContext context,
        CancellationToken cancellationToken)
    {
        Process[] processes = Process.GetProcessesByName("Revit");
        foreach (Process process in processes)
        {
            context.Logger.LogInformation("Stopping Revit process {ProcessId}", process.Id);

            if (process.HasExited)
                continue;

            if (!process.CloseMainWindow())
            {
                context.Logger.LogWarning(
                    "Revit process {ProcessId} did not respond to a close request, terminating it",
                    process.Id);
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(cancellationToken);
                continue;
            }

            if (!await WaitForExitAsync(process, CloseTimeout, cancellationToken))
            {
                context.Logger.LogWarning(
                    "Revit process {ProcessId} did not exit within {Timeout}, terminating it",
                    process.Id,
                    CloseTimeout);
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(cancellationToken);
            }
        }

        return null;
    }

    private static async Task<bool> WaitForExitAsync(
        Process process,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);

        try
        {
            await process.WaitForExitAsync(timeoutSource.Token);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
