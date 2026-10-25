using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Revit.Linter.Architecture.Tests;

/// <summary>
/// Guards the weak coupling between UI presenter modules: a new dependency on another presenter's project,
/// view or view model must be an explicit architectural decision recorded in the allowed lists below and in
/// <c>docs/architecture.md</c>, not an incidental import.
/// </summary>
public sealed class UiModuleCouplingTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    // UI modules whose cross-module dependencies are part of the documented composition points.
    private static readonly Dictionary<string, string[]> AllowedPresenterReferences = new(StringComparer.Ordinal)
    {
        ["Revit.Linter.DiagnosticListPresenter"] = ["Revit.Linter.DialogPresenter", "Revit.Linter.RunDiagnosticPresenter"],
        ["Revit.Linter.DiagnosticReportPresenter"] = ["Revit.Linter.DialogPresenter", "Revit.Linter.OpenedDocuments", "Revit.Linter.RunDiagnosticPresenter"],
        ["Revit.Linter.FixReportPresenter"] = ["Revit.Linter.OpenedDocuments", "Revit.Linter.RunDiagnosticPresenter"],
        ["Revit.Linter.IgnoreListPresenter"] = ["Revit.Linter.DialogPresenter", "Revit.Linter.RunDiagnosticPresenter"],
        ["Revit.Linter.RunDiagnosticPresenter"] = ["Revit.Linter.DialogPresenter"],
        ["Revit.Linter.OpenedDocuments"] = [],
        ["Revit.Linter.DialogPresenter"] = [],
        ["Revit.Linter.PracticalTourPresenter"] = [],
        ["Revit.Linter.WelcomePresenter"] = [],
    };

    private static readonly Dictionary<string, string[]> PresenterViewModelTypes = new(StringComparer.Ordinal)
    {
        ["Revit.Linter.DiagnosticListPresenter"] = ["DiagnosticListViewModel"],
        ["Revit.Linter.DiagnosticReportPresenter"] = ["DiagnosticReportViewModel"],
        ["Revit.Linter.FixReportPresenter"] = ["FixReportViewModel"],
        ["Revit.Linter.IgnoreListPresenter"] = ["IgnoreListViewModel"],
        ["Revit.Linter.RunDiagnosticPresenter"] = ["RunDiagnosticViewModel"],
        ["Revit.Linter.OpenedDocuments"] = ["OpenedDocumentsViewModel"],
        ["Revit.Linter.DialogPresenter"] = ["DialogViewModel", "ConfirmationDialogViewModel"],
        ["Revit.Linter.PracticalTourPresenter"] = ["PracticalTourViewModel"],
        ["Revit.Linter.WelcomePresenter"] = ["WelcomeViewModel"],
    };

    // The only allowed cross-module view-model composition: every pane view hosts the shared run control.
    private static readonly (string View, string Module)[] AllowedXamlComposition =
    [
        ("src/Revit.Linter.DiagnosticListPresenter/Views/DiagnosticListView.xaml", "Revit.Linter.RunDiagnosticPresenter"),
        ("src/Revit.Linter.DiagnosticReportPresenter/Views/DiagnosticReportView.xaml", "Revit.Linter.RunDiagnosticPresenter"),
        ("src/Revit.Linter.FixReportPresenter/Views/FixReportView.xaml", "Revit.Linter.RunDiagnosticPresenter"),
        ("src/Revit.Linter.IgnoreListPresenter/Views/IgnoreListView.xaml", "Revit.Linter.RunDiagnosticPresenter"),
    ];

    [Fact]
    public void PresenterProjects_ReferenceOnlyAllowedUiModules()
    {
        var uiModules = AllowedPresenterReferences.Keys.ToArray();
        foreach (var pair in AllowedPresenterReferences)
        {
            string project = pair.Key;
            string[] allowed = pair.Value;
            string[] actual = ProjectReferences(project)
                .Where(uiModules.Contains)
                .OrderBy(module => module, StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(allowed.OrderBy(module => module, StringComparer.Ordinal), actual);
        }
    }

    [Fact]
    public void PresenterCode_DoesNotUseAnotherPresentersViewModel()
    {
        foreach (var pair in PresenterViewModelTypes)
        {
            string project = pair.Key;
            string[] ownTypes = pair.Value;
            string[] otherViewModelTypes = PresenterViewModelTypes
                .Where(pair => !string.Equals(pair.Key, project, StringComparison.Ordinal))
                .SelectMany(pair => pair.Value)
                .Except(ownTypes)
                .ToArray();

            foreach (string file in ProjectFiles(project, "*.cs"))
            {
                string content = WithoutStringLiterals(File.ReadAllText(file));
                foreach (string viewModelType in otherViewModelTypes)
                {
                    if (!ContainsWord(content, viewModelType)) continue;

                    Assert.Fail(
                        $"{RelativePath(file)} uses the '{viewModelType}' of another UI module. "
                        + "Depend on that module's contract instead, or record an allowed composition point.");
                }
            }
        }
    }

    [Fact]
    public void Xaml_ComposesViewModelsOnlyAtTheAllowedPoints()
    {
        var expected = AllowedXamlComposition
            .OrderBy(point => point.View, StringComparer.Ordinal)
            .ToArray();
        var actual = new List<(string View, string Module)>();
        Regex composition = new(@"clr-namespace:(?<module>Revit\.Linter\.[A-Za-z.]+?)\.ViewModels;assembly=");

        foreach (string file in Directory.EnumerateFiles(Path.Combine(RepositoryRoot, "src"), "*.xaml", SearchOption.AllDirectories))
        {
            if (IsBuildOutput(file)) continue;

            string project = ProjectOf(file);
            string content = File.ReadAllText(file);
            foreach (Match match in composition.Matches(content))
            {
                if (string.Equals(match.Groups["module"].Value, project, StringComparison.Ordinal)) continue;
                actual.Add((RelativePath(file), match.Groups["module"].Value));
            }
        }

        Assert.Equal(
            expected,
            actual.OrderBy(point => point.View, StringComparer.Ordinal).ToArray());
    }

    private static IEnumerable<string> ProjectReferences(string project)
    {
        string projectFile = Path.Combine(RepositoryRoot, "src", project, project + ".csproj");
        XDocument document = XDocument.Load(projectFile);
        return document.Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => value is not null)
            .Select(value => Path.GetFileNameWithoutExtension(value!));
    }

    private static IEnumerable<string> ProjectFiles(string project, string pattern)
    {
        string projectDirectory = Path.Combine(RepositoryRoot, "src", project);
        return Directory.EnumerateFiles(projectDirectory, pattern, SearchOption.AllDirectories)
            .Where(file => !IsBuildOutput(file));
    }

    private static bool IsBuildOutput(string file)
    {
        string relative = RelativePath(file);
        return relative.Contains("/bin/", StringComparison.Ordinal)
            || relative.Contains("/obj/", StringComparison.Ordinal);
    }

    private static string ProjectOf(string file)
    {
        string relative = RelativePath(file);
        string[] segments = relative.Split('/');
        return segments[1];
    }

    private static string RelativePath(string file)
    {
        string separator = Path.DirectorySeparatorChar.ToString();
        string root = RepositoryRoot.EndsWith(separator, StringComparison.Ordinal)
            ? RepositoryRoot
            : RepositoryRoot + separator;
        string relative = file.StartsWith(root, StringComparison.OrdinalIgnoreCase)
            ? file.Substring(root.Length)
            : file;
        return relative.Replace('\\', '/');
    }

    private static bool ContainsWord(string content, string word) =>
        Regex.IsMatch(content, $@"\b{Regex.Escape(word)}\b", RegexOptions.CultureInvariant);

    // Resource and localization names legitimately repeat a module and type name in a string literal; only
    // real type usage outside those literals couples the modules.
    private static string WithoutStringLiterals(string content) =>
        Regex.Replace(content, "\"(?:[^\"\\\\]|\\\\.)*\"", "\"\"", RegexOptions.CultureInvariant);

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Revit.Linter.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "The repository root was not found above the test output directory.");
    }
}
