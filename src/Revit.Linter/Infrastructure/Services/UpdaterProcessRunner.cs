using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Revit.Linter.Core.Abstractions;

namespace Revit.Linter.Infrastructure.Services;

/// <summary>
/// Starts the standalone updater for the add-in and resolves its installed location.
/// </summary>
/// <remarks>
/// This is the single place where the add-in starts the updater. Release discovery, state and
/// notifications stay in the updater process; the add-in only launches it and, for a startup check,
/// waits for its one-shot result file.
/// </remarks>
internal static class UpdaterProcessRunner
{
    /// <summary>Starts the updater without waiting for it to finish.</summary>
    /// <param name="arguments">The command-line arguments for the updater.</param>
    public static void Start(string arguments)
    {
        using Process? process = Process.Start(CreateStartInfo(arguments));
    }

    /// <summary>Starts the updater and waits until it exits, the timeout elapses or the caller cancels.</summary>
    /// <param name="arguments">The command-line arguments for the updater.</param>
    /// <param name="timeout">The maximum time to wait for the updater.</param>
    /// <param name="cancellationToken">A token that stops the wait and the updater process.</param>
    /// <returns><see langword="true"/> when the updater exited; otherwise <see langword="false"/>.</returns>
    public static async Task<bool> RunAndWaitAsync(
        string arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        using Process process = new()
        {
            StartInfo = CreateStartInfo(arguments),
            EnableRaisingEvents = true
        };
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        process.Exited += (_, _) => completion.TrySetResult(true);

        if (!process.Start())
            return false;
        if (process.HasExited)
            completion.TrySetResult(true);

        using var timeoutSource = new CancellationTokenSource(timeout);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutSource.Token);
        using CancellationTokenRegistration registration = linkedSource.Token.Register(() =>
        {
            try
            {
                process.Kill();
            }
            catch (Exception exception) when (
                exception is InvalidOperationException or Win32Exception or NotSupportedException)
            {
                // The process already exited or cannot be terminated; the wait still completes.
            }

            completion.TrySetCanceled(linkedSource.Token);
        });

        try
        {
            await completion.Task.ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private static ProcessStartInfo CreateStartInfo(string arguments)
    {
        string executablePath = ResolveExecutablePath();
        return new ProcessStartInfo(executablePath)
        {
            Arguments = arguments,
            WorkingDirectory = Path.GetDirectoryName(executablePath),
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };
    }

    private static string ResolveExecutablePath()
    {
        string installedPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs",
            ProductIdentity.InstallVendor,
            ProductIdentity.AddInName,
            ProductIdentity.UpdaterExecutableName);
        if (File.Exists(installedPath))
            return installedPath;

        string? assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string adjacentPath = Path.Combine(assemblyDirectory ?? string.Empty, ProductIdentity.UpdaterExecutableName);
        if (File.Exists(adjacentPath))
            return adjacentPath;

        throw new FileNotFoundException("The Revit Linter updater executable was not found.", installedPath);
    }
}
