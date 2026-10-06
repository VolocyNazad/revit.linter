using Revit.Linter.WelcomePresenter.Abstractions.Models;
using Revit.Linter.WelcomePresenter.Abstractions.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.IO;

namespace Revit.Linter.WelcomePresenter.Infrastructure.Services;

/// <summary>Copies a tutorial source into a unique directory owned by Revit Linter.</summary>
internal sealed class TutorialSampleCopyService(ILogger<TutorialSampleCopyService>? logger = null)
    : ITutorialSampleCopyService, IDisposable
{
    private readonly Dictionary<string, Mutex> _sessionLocks = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _syncRoot = new();
    private readonly ILogger<TutorialSampleCopyService> _logger = logger ?? NullLogger<TutorialSampleCopyService>.Instance;

    public string CreateCopy(TutorialSampleCandidate candidate, int revitVersion)
    {
        string destination = TutorialCopyPathPlanner.Create(candidate.SourcePath, revitVersion);
        string sessionDirectory = Path.GetDirectoryName(destination)
                                  ?? throw new InvalidOperationException("Tutorial session directory is unavailable.");
        Directory.CreateDirectory(sessionDirectory);

        try
        {
            Mutex sessionLock = new(initiallyOwned: true, GetMutexName(sessionDirectory));
            lock (_syncRoot) _sessionLocks.Add(sessionDirectory, sessionLock);
            File.Copy(candidate.SourcePath, destination, overwrite: false);
            _logger.LogInformation(
                "Prepared tutorial sample copy {TutorialSamplePath} from {TutorialSampleSource}",
                destination,
                candidate.SourcePath);
            return destination;
        }
        catch
        {
            ReleaseLock(sessionDirectory);
            try
            {
                if (Directory.Exists(sessionDirectory)) Directory.Delete(sessionDirectory, recursive: true);
            }
            catch (Exception cleanupException) when (cleanupException is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(
                    cleanupException,
                    "Failed to remove an incomplete tutorial session {TutorialSessionDirectory}",
                    sessionDirectory);
            }
            throw;
        }
    }

    public void DeleteCopy(string path, int revitVersion)
    {
        if (!TutorialCopyPathPlanner.TryGetSessionDirectory(path, revitVersion, out string? sessionDirectory)
            || sessionDirectory is null)
        {
            _logger.LogWarning("Refused to delete tutorial copy outside the owned root: {TutorialSamplePath}", path);
            return;
        }

        try
        {
            ReleaseLock(sessionDirectory);
            if (Directory.Exists(sessionDirectory)) Directory.Delete(sessionDirectory, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(exception, "Failed to delete tutorial session {TutorialSessionDirectory}", sessionDirectory);
        }
    }

    public void CleanupAbandonedCopies(int revitVersion)
    {
        string versionRoot = TutorialCopyPathPlanner.GetVersionRoot(revitVersion);
        if (!Directory.Exists(versionRoot)) return;

        try
        {
            foreach (string sessionDirectory in Directory.EnumerateDirectories(versionRoot))
            {
                if (!Guid.TryParseExact(Path.GetFileName(sessionDirectory), "N", out _)) continue;

                Mutex? sessionLock = null;
                try
                {
                    sessionLock = new Mutex(initiallyOwned: false, GetMutexName(sessionDirectory));
                    if (!TryAcquire(sessionLock)) continue;
                    Directory.Delete(sessionDirectory, recursive: true);
                }
                finally
                {
                    if (sessionLock is not null)
                    {
                        try { sessionLock.ReleaseMutex(); }
                        catch (ApplicationException) { /* The mutex belongs to another Revit process. */ }
                        sessionLock.Dispose();
                    }
                }
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(exception, "Failed to clean abandoned tutorial sessions below {TutorialRoot}", versionRoot);
        }
    }

    public void Dispose()
    {
        lock (_syncRoot)
        {
            foreach (Mutex sessionLock in _sessionLocks.Values)
            {
                sessionLock.ReleaseMutex();
                sessionLock.Dispose();
            }
            _sessionLocks.Clear();
        }
    }

    private void ReleaseLock(string sessionDirectory)
    {
        lock (_syncRoot)
        {
            if (!_sessionLocks.TryGetValue(sessionDirectory, out Mutex? sessionLock)) return;
            _sessionLocks.Remove(sessionDirectory);
            sessionLock.ReleaseMutex();
            sessionLock.Dispose();
        }
    }

    private static string GetMutexName(string sessionDirectory)
        => $"Local\\RevitLinterTutorial_{Path.GetFileName(sessionDirectory)}";

    private static bool TryAcquire(Mutex sessionLock)
    {
        try
        {
            return sessionLock.WaitOne(0);
        }
        catch (AbandonedMutexException)
        {
            return true;
        }
    }
}
