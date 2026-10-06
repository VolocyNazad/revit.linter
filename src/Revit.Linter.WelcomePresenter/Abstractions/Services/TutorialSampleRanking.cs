using Revit.Linter.WelcomePresenter.Abstractions.Models;
using System.IO;

namespace Revit.Linter.WelcomePresenter.Abstractions.Services;

/// <summary>Provides the shared naming and ordering convention for official Autodesk tutorial samples.</summary>
public static class TutorialSampleRanking
{
    /// <summary>Infers a supported discipline from the conventional Autodesk sample file name.</summary>
    /// <param name="fileName">The sample file name, with or without its extension.</param>
    /// <returns>The inferred discipline, or <see langword="null"/> when the name is not recognized.</returns>
    public static ExampleDiscipline? InferDiscipline(string fileName)
    {
        string name = Path.GetFileNameWithoutExtension(fileName);
        if (ContainsAny(name, "mep", "mechanical", "electrical", "plumbing", "rme"))
            return ExampleDiscipline.Mep;
        if (ContainsAny(name, "architecture", "architectural", "rac"))
            return ExampleDiscipline.Architecture;
        if (ContainsAny(name, "structure", "structural", "rst"))
            return ExampleDiscipline.Structure;
        return null;
    }

    /// <summary>Places samples of the preferred disciplines first and then orders them by file name.</summary>
    /// <param name="candidates">Compatible sample candidates from the current Revit installation.</param>
    /// <param name="preferredDisciplines">Disciplines selected in the welcome window.</param>
    /// <returns>A materialized ordered collection.</returns>
    public static IReadOnlyList<TutorialSampleCandidate> Order(
        IEnumerable<TutorialSampleCandidate> candidates,
        IReadOnlyCollection<ExampleDiscipline> preferredDisciplines)
    {
        HashSet<ExampleDiscipline> preferred = [.. preferredDisciplines];
        return candidates
            .OrderBy(candidate => candidate.Discipline is not null && preferred.Contains(candidate.Discipline.Value) ? 0 : 1)
            .ThenBy(candidate => candidate.Discipline is null ? 1 : 0)
            .ThenBy(candidate => candidate.FileName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private static bool ContainsAny(string value, params string[] candidates) =>
        candidates.Any(candidate => value.Contains(candidate, StringComparison.OrdinalIgnoreCase));
}
