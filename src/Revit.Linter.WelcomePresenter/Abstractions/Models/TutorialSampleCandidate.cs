namespace Revit.Linter.WelcomePresenter.Abstractions.Models;

/// <summary>Describes one compatible Autodesk sample that may be copied for a practical tour.</summary>
/// <param name="SourcePath">The full path to the original Autodesk sample.</param>
/// <param name="FileName">The file name shown to the user.</param>
/// <param name="Discipline">The discipline inferred from the official sample file name, when recognized.</param>
/// <param name="Format">The Revit file format reported by <c>BasicFileInfo</c>.</param>
public sealed record TutorialSampleCandidate(
    string SourcePath,
    string FileName,
    ExampleDiscipline? Discipline,
    string Format = "");
