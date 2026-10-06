using Revit.Linter.WelcomePresenter.Abstractions.Models;
using Revit.Linter.WelcomePresenter.Abstractions.Services;
using Revit.Linter.WelcomePresenter.Infrastructure.Services;

namespace Revit.Linter.WelcomePresenter.Tests;

public sealed class TutorialSampleTests : IDisposable
{
    private readonly List<string> _sessionDirectories = [];

    [Theory]
    [InlineData("rme_basic_sample_project.rvt", ExampleDiscipline.Mep)]
    [InlineData("rac_basic_sample_project.rvt", ExampleDiscipline.Architecture)]
    [InlineData("rst_basic_sample_project.rvt", ExampleDiscipline.Structure)]
    public void Infers_a_discipline_from_conventional_sample_names(
        string fileName, ExampleDiscipline expectedDiscipline)
    {
        Assert.Equal(expectedDiscipline, TutorialSampleRanking.InferDiscipline(fileName));
    }

    [Fact]
    public void Places_the_preferred_discipline_first()
    {
        TutorialSampleCandidate[] candidates =
        [
            Candidate("rac_basic_sample_project.rvt"),
            Candidate("rst_basic_sample_project.rvt"),
            Candidate("rme_basic_sample_project.rvt"),
        ];

        IReadOnlyList<TutorialSampleCandidate> ordered = TutorialSampleRanking.Order(
            candidates, [ExampleDiscipline.Structure]);

        Assert.Equal("rst_basic_sample_project.rvt", ordered[0].FileName);
    }

    [Fact]
    public void Creates_a_fresh_path_below_the_owned_tutorial_root()
    {
        string source = Path.Combine("C:", "Program Files", "Autodesk", "Revit 2025", "Samples", "sample.rvt");

        string first = TutorialCopyPathPlanner.Create(source, 2025);
        string second = TutorialCopyPathPlanner.Create(source, 2025);

        string expectedRoot = Path.Combine(Path.GetTempPath(), "Revit Linter", "Tutorial", "2025");
        Assert.StartsWith(expectedRoot, first, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("sample.rvt", first, StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Creates_a_new_copy_without_modifying_the_source()
    {
        string sourceDirectory = Path.Combine(Path.GetTempPath(), nameof(TutorialSampleTests), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sourceDirectory);
        string source = Path.Combine(sourceDirectory, "sample.rvt");
        File.WriteAllText(source, "original");
        _sessionDirectories.Add(sourceDirectory);
        TutorialSampleCandidate candidate = new(source, "sample.rvt", ExampleDiscipline.Architecture);

        string copy = new TutorialSampleCopyService().CreateCopy(candidate, 2025);
        _sessionDirectories.Add(Path.GetDirectoryName(copy)!);

        Assert.Equal("original", File.ReadAllText(source));
        Assert.Equal("original", File.ReadAllText(copy));
        Assert.NotEqual(source, copy);
    }

    public void Dispose()
    {
        foreach (string directory in _sessionDirectories.OrderByDescending(path => path.Length))
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }

    private static TutorialSampleCandidate Candidate(string fileName) =>
        new(fileName, fileName, TutorialSampleRanking.InferDiscipline(fileName));
}
