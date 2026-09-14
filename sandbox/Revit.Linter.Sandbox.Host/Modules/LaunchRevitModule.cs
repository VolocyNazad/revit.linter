using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.Modules;
using Revit.Linter.Sandbox.Host.Models;

namespace Revit.Linter.Sandbox.Host.Modules;

[SupportedOSPlatform("windows")]
[DependsOn<CreateManifestModule>]
public sealed class LaunchRevitModule : Module<object?>
{
    protected override async Task<object?> ExecuteAsync(
        IModuleContext context,
        CancellationToken cancellationToken)
    {
        ManifestInfo manifest = (await context.GetModule<CreateManifestModule>()).ValueOrDefault!;
        string revitPath = SandboxPaths.FindRevit(int.Parse(manifest.MajorVersion));

        context.Logger.LogInformation("Launching Revit: {RevitPath}", revitPath);
        using var revit = Process.Start(new ProcessStartInfo
        {
            FileName = revitPath,
            WorkingDirectory = Path.GetDirectoryName(revitPath),
            UseShellExecute = false,
        }) ?? throw new InvalidOperationException($"Failed to start Revit: {revitPath}");

        if (Debugger.IsAttached && !AttachDebugger(revit.Id, context.Logger))
        {
            context.Logger.LogWarning(
                "Failed to attach the debugger to Revit process {ProcessId}; " +
                "attach it manually via Debug > Attach to Process",
                revit.Id);
        }

        await revit.WaitForExitAsync(cancellationToken);

        context.Logger.LogInformation("Revit exited with code {ExitCode}", revit.ExitCode);
        return null;
    }

    private static bool AttachDebugger(int processId, ILogger logger)
    {
        foreach (Process devenv in Process.GetProcessesByName("devenv"))
        {
            try
            {
                object? dte = GetRunningDte(devenv.Id);
                if (dte is null)
                    continue;

                dynamic debugger = dte.GetType().InvokeMember(
                    "Debugger",
                    System.Reflection.BindingFlags.GetProperty,
                    null, dte, null)!;

                foreach (dynamic process in debugger.LocalProcesses)
                {
                    if (process.ProcessID == processId)
                    {
                        process.Attach();
                        logger.LogInformation(
                            "Debugger attached to Revit process {ProcessId} via {VisualStudioProcessId}",
                            processId, devenv.Id);
                        return true;
                    }
                }
            }
            catch (Exception exception)
            {
                logger.LogDebug(
                    "Failed to attach debugger via Visual Studio process {VisualStudioProcessId}: {Exception}",
                    devenv.Id, exception);
            }
        }

        return false;
    }

    private static object? GetRunningDte(int devenvProcessId)
    {
        GetRunningObjectTable(0, out IRunningObjectTable table);
        table.EnumRunning(out IEnumMoniker monikers);

        IMoniker[] single = new IMoniker[1];
        while (monikers.Next(1, single, IntPtr.Zero) == 0)
        {
            try
            {
                single[0].GetDisplayName(null!, null!, out string displayName);
                if (displayName.EndsWith($":{devenvProcessId}", StringComparison.Ordinal))
                {
                    Guid iid = typeof(object).GUID;
                    single[0].BindToObject(null!, null!, ref iid, out object? dte);
                    if (dte is not null)
                        return dte;
                }
            }
            catch
            {
                // The moniker does not support binding; skip it.
            }
            finally
            {
                Marshal.ReleaseComObject(single[0]);
            }
        }

        return null;
    }

    [DllImport("ole32.dll")]
    private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);
}
