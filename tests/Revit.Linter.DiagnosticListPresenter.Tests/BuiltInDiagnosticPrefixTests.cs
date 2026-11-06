using System.Text.RegularExpressions;
using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.DiagnosticListPresenter.Tests;

/// <summary>
/// Guards the discipline tag table: every code prefix a built-in collector declares must have an
/// explicit entry, so a new prefix cannot appear without a tag-or-none decision.
/// </summary>
public sealed class BuiltInDiagnosticPrefixTests
{
    private static readonly string[] _collectorFiles =
    [
        Path.Combine("src", "Revit.Linter.ElementDiagnostics", "ElementDiagnosticIdCollector.cs"),
        Path.Combine("src", "Revit.Linter.DocumentDiagnostics", "DocumentDiagnosticIdCollector.cs"),
    ];

    [Fact]
    public void Every_built_in_diagnostic_prefix_has_a_discipline_entry()
    {
        string root = FindRepositoryRoot();
        Regex codePattern = new(@"Create\(""(?<code>[A-Z]+\d+)""");
        List<string> codes = [];
        foreach (string relativePath in _collectorFiles)
        {
            string content = File.ReadAllText(Path.Combine(root, relativePath));
            codes.AddRange(codePattern.Matches(content).Cast<Match>().Select(match => match.Groups["code"].Value));
        }

        Assert.NotEmpty(codes);
        string[] unknown = codes
            .Where(code => !DiagnosticTags.TryGetDisciplineTag(code, out _))
            .Select(CodePrefix)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            unknown.Length == 0,
            "Add the prefix to the discipline table in DiagnosticTags.cs with its tag or an explicit "
            + $"no-discipline entry: {string.Join(", ", unknown)}.");
    }

    private static string CodePrefix(string code)
    {
        int length = 0;
        while (length < code.Length && char.IsLetter(code[length])) length++;
        return code.Substring(0, length);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Revit.Linter.slnx")))
            directory = directory.Parent;

        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
