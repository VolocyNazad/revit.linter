using Autodesk.Revit.DB;
using Microsoft.Extensions.Logging;
using Revit.Linter.WelcomePresenter.Abstractions.Models;
using Revit.Linter.WelcomePresenter.Abstractions.Services;
using System.Diagnostics;
using System.IO;

namespace Revit.Linter.Infrastructure.Services;

/// <summary>Finds project samples shipped beside the currently running Revit executable.</summary>
internal sealed class AutodeskTutorialSampleCatalog(ILogger<AutodeskTutorialSampleCatalog> logger)
    : ITutorialSampleCatalog
{
    public IReadOnlyList<TutorialSampleCandidate> Find(
        IReadOnlyCollection<ExampleDiscipline> preferredDisciplines)
    {
        try
        {
            string? executable = Process.GetCurrentProcess().MainModule?.FileName;
            string? installationDirectory = Path.GetDirectoryName(executable);
            if (installationDirectory is null) return [];

            string samplesDirectory = Path.Combine(installationDirectory, "Samples");
            if (!Directory.Exists(samplesDirectory)) return [];

            List<TutorialSampleCandidate> candidates = [];
            foreach (string path in Directory.EnumerateFiles(samplesDirectory, "*.rvt", SearchOption.AllDirectories))
            {
                TutorialSampleCandidate? candidate = TryCreateCandidate(path);
                if (candidate is not null) candidates.Add(candidate);
            }

            return TutorialSampleRanking.Order(candidates, preferredDisciplines);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Autodesk tutorial samples could not be discovered");
            return [];
        }
    }

    private TutorialSampleCandidate? TryCreateCandidate(string path)
    {
        try
        {
            using BasicFileInfo fileInfo = BasicFileInfo.Extract(path);
            if (!fileInfo.IsSavedInCurrentVersion || fileInfo.IsSavedInLaterVersion || fileInfo.IsWorkshared)
                return null;

            string fileName = Path.GetFileName(path);
            return new TutorialSampleCandidate(
                path,
                fileName,
                TutorialSampleRanking.InferDiscipline(fileName),
                fileInfo.Format);
        }
        catch (Exception exception) when (exception is IOException
                                              or UnauthorizedAccessException
                                              or Autodesk.Revit.Exceptions.ApplicationException)
        {
            logger.LogDebug(exception, "Skipped incompatible Autodesk tutorial sample {SamplePath}", path);
            return null;
        }
    }
}
