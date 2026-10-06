using Revit.Linter.WelcomePresenter.Abstractions.Models;

namespace Revit.Linter.WelcomePresenter.Abstractions.Services;

/// <summary>Creates disposable, per-session copies of official Autodesk tutorial samples.</summary>
public interface ITutorialSampleCopyService
{
    /// <summary>Copies the selected sample into a new application-owned temporary session directory.</summary>
    /// <param name="candidate">The compatible Autodesk sample selected by the user.</param>
    /// <param name="revitVersion">The running Revit major version.</param>
    /// <returns>The full path to the new copy.</returns>
    /// <remarks>The source file is never opened for writing, overwritten or removed.</remarks>
    string CreateCopy(TutorialSampleCandidate candidate, int revitVersion);

    /// <summary>Deletes the session directory that owns a prepared tutorial copy.</summary>
    /// <remarks>Paths outside the Revit Linter tutorial root are rejected and left untouched.</remarks>
    void DeleteCopy(string path, int revitVersion);

    /// <summary>Removes abandoned session directories left by earlier Revit processes.</summary>
    void CleanupAbandonedCopies(int revitVersion);
}
