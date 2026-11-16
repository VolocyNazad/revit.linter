using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Revit.Linter.Architecture.Tests;

/// <summary>
/// Guards the design-time data policy: every view declares a designer data context that the view locator can
/// find, every binding path of the view resolves on the design data of its presenter project, and every
/// localized string the design data reads exists in its resource file.
/// </summary>
/// <remarks>
/// The checks read the repository sources rather than the built assemblies, so a broken designer preview fails
/// the build before anybody opens Visual Studio. A binding root is matched against the union of the members
/// that the design data of one presenter project provides, including the rows of its item templates.
/// </remarks>
public sealed class DesignDataTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();
    private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
    private static readonly XNamespace Design = "http://schemas.microsoft.com/expression/blend/2008";

    private static readonly Regex BindingExpression = new(
        @"\{\s*Binding\s*(?<body>(?:[^{}]|\{[^{}]*\})*)\}",
        RegexOptions.CultureInvariant);

    private static readonly Regex NestedMarkupExtension = new(@"\{[^{}]*\}", RegexOptions.CultureInvariant);

    private static readonly Regex DesignInstanceType = new(
        @"\{x:Type\s+(?<prefix>[A-Za-z_]\w*):(?<type>[A-Za-z_]\w*)\}",
        RegexOptions.CultureInvariant);

    private static readonly Regex ClrNamespace = new(
        @"^clr-namespace:(?<namespace>[^;]+)(?:;assembly=(?<assembly>[^;]+))?$",
        RegexOptions.CultureInvariant);

    private static readonly Regex Word = new(@"[A-Za-z_]\w*", RegexOptions.CultureInvariant);

    private static readonly Regex StringLiteral = new("\"(?:[^\"\\\\]|\\\\.)*\"", RegexOptions.CultureInvariant);

    private static readonly Regex InlineResourceRead = new(
        @"GetString\(\s*(?<owner>ResourceBaseName|""(?<base>[^""]+)"")\s*,\s*""(?<key>[^""]+)""\s*\)",
        RegexOptions.CultureInvariant);

    private static readonly Regex TypedResourceRead = new(
        @"\b(?<localizer>[A-Za-z_]\w*Localizations)\.GetString\(\s*""(?<key>[^""]+)""\s*\)",
        RegexOptions.CultureInvariant);

    private static readonly string[] SkippedBindingOptions = ["Source=", "ElementName=", "RelativeSource="];

    private static readonly Lazy<IReadOnlyList<ViewFile>> Views = new(LoadViews);
    private static readonly Lazy<IReadOnlyDictionary<string, ProjectDesignData>> Projects = new(LoadProjects);
    private static readonly Lazy<IReadOnlyDictionary<string, HashSet<string>>> ResourceKeys = new(LoadResourceKeys);

    [Fact]
    public void EveryView_DeclaresDesignerDataThatTheViewLocatorCanFind()
    {
        foreach (ViewFile view in Views.Value)
        {
            string viewClass = view.Document.Root!.Attribute(Xaml + "Class")!.Value;
            int separator = viewClass.LastIndexOf('.');
            string viewNamespace = viewClass.Substring(0, separator);
            string viewName = viewClass.Substring(separator + 1);

            XAttribute? context = view.Document.Root.Attribute(Design + "DataContext");
            Assert.True(
                context is not null,
                $"{view.RelativePath}: the root declares no d:DataContext, so the XAML designer renders the view without data.");
            Assert.Contains("d:DesignInstance", context.Value, StringComparison.Ordinal);
            Assert.Contains("IsDesignTimeCreatable=True", context.Value, StringComparison.Ordinal);

            Match type = DesignInstanceType.Match(context.Value);
            Assert.True(
                type.Success,
                $"{view.RelativePath}: d:DataContext does not name its design data through {{x:Type prefix:Type}}.");

            string prefix = type.Groups["prefix"].Value;
            string declaredType = type.Groups["type"].Value;
            XAttribute? namespaceDeclaration = view.Document.Root.Attribute(XNamespace.Xmlns + prefix);
            Assert.True(
                namespaceDeclaration is not null,
                $"{view.RelativePath}: the design data prefix '{prefix}' has no clr-namespace declaration.");

            Match clrNamespace = ClrNamespace.Match(namespaceDeclaration.Value);
            Assert.True(
                clrNamespace.Success,
                $"{view.RelativePath}: the design data prefix '{prefix}' is not a clr-namespace declaration.");

            string designNamespace = clrNamespace.Groups["namespace"].Value;
            string? assembly = clrNamespace.Groups["assembly"].Success ? clrNamespace.Groups["assembly"].Value : null;
            Assert.True(
                assembly is null || string.Equals(assembly, view.Project, StringComparison.Ordinal),
                $"{view.RelativePath}: the design data must live in the presenter project itself, not in '{assembly}'.");

            string expectedName = viewName.EndsWith("View", StringComparison.Ordinal)
                ? viewName.Substring(0, viewName.Length - "View".Length) + "DesignData"
                : viewName + "DesignData";
            string expectedNamespace = viewNamespace.Replace(".Views", ".DesignData");

            Assert.Equal(expectedNamespace, designNamespace);
            Assert.Equal(expectedName, declaredType);

            Assert.True(
                Projects.Value.TryGetValue(view.Project, out ProjectDesignData? project)
                && project.Types.ContainsKey($"{designNamespace}.{declaredType}"),
                $"{view.RelativePath}: the view locator cannot find '{designNamespace}.{declaredType}'. "
                + "Name the class after the view model without its 'ViewModel' suffix and put it in the DesignData folder.");
        }
    }

    [Fact]
    public void EveryBindingPath_ResolvesOnTheDesignDataOfItsPresenter()
    {
        foreach (ViewFile view in Views.Value)
        {
            if (!Projects.Value.TryGetValue(view.Project, out ProjectDesignData? project))
            {
                Assert.Fail(
                    $"{view.RelativePath}: the project '{view.Project}' has no DesignData folder, "
                    + "so the XAML designer renders the view without data.");
                continue;
            }

            var failures = new List<string>();
            foreach ((string root, string expression, int line) in BindingRoots(view))
            {
                if (project.Members.Contains(root)) continue;
                failures.Add($"{view.RelativePath}:{line} binds '{expression}', but no design data member '{root}' exists.");
            }

            Assert.True(
                failures.Count == 0,
                "The XAML designer binds to a member that the design data does not provide:" + Environment.NewLine
                + string.Join(Environment.NewLine, failures));
        }
    }

    [Fact]
    public void DesignData_ReadsOnlyResourceKeysThatExist()
    {
        IReadOnlyDictionary<string, HashSet<string>> resourceKeys = ResourceKeys.Value;
        var failures = new List<string>();

        foreach (string file in DesignFiles())
        {
            string text = File.ReadAllText(file);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(text, path: file, cancellationToken: TestContext.Current.CancellationToken);

            foreach (TypeDeclarationSyntax type in tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                string typeText = type.ToFullString();
                int typeOffset = type.SpanStart;
                string? baseName = ConstantString(type, "ResourceBaseName");
                foreach (string helper in ResourceHelpers(type))
                {
                    foreach (Match call in Regex.Matches(
                                 typeText,
                                 $@"\b{Regex.Escape(helper)}\(\s*""(?<key>[^""]+)""\s*\)",
                                 RegexOptions.CultureInvariant))
                        Check(file, text, typeOffset + call.Index, type, baseName, call, resourceKeys, failures);
                }

                foreach (Match read in InlineResourceRead.Matches(typeText))
                {
                    if (read.Groups["owner"].Value == "ResourceBaseName" && baseName is null)
                    {
                        failures.Add(
                            $"{ToRelativePath(file)}:{LineOf(text, read.Index)}: {type.Identifier.ValueText} reads "
                            + "ResourceBaseName but declares no such constant.");
                        continue;
                    }

                    Check(file, text, typeOffset + read.Index, type, baseName, read, resourceKeys, failures);
                }
            }

            foreach (Match read in TypedResourceRead.Matches(text))
                CheckTyped(file, text, read, resourceKeys, failures);
        }

        Assert.True(
            failures.Count == 0,
            "The design data reads a localization key that its resource file does not declare:" + Environment.NewLine
            + string.Join(Environment.NewLine, failures));
    }

    private static IEnumerable<string> ResourceHelpers(TypeDeclarationSyntax type) =>
        type.Members.OfType<MethodDeclarationSyntax>()
            .Where(method => method.ParameterList.Parameters.Any(parameter => parameter.Type?.ToString() == "string")
                && method.ToString().IndexOf("GetString(ResourceBaseName", StringComparison.Ordinal) >= 0)
            .Select(method => method.Identifier.ValueText);

    private static void Check(
        string file,
        string text,
        int offset,
        TypeDeclarationSyntax type,
        string? baseName,
        Match read,
        IReadOnlyDictionary<string, HashSet<string>> resourceKeys,
        List<string> failures)
    {
        string? owner = read.Groups["base"].Success ? read.Groups["base"].Value : baseName;
        if (owner is null) return;

        string resource = ResourceName(owner);
        string position = $"{ToRelativePath(file)}:{LineOf(text, offset)}: {type.Identifier.ValueText}";
        if (!resourceKeys.TryGetValue(resource, out HashSet<string>? keys))
        {
            failures.Add($"{position} reads '{resource}', but no resource file with that name exists.");
            return;
        }

        string key = read.Groups["key"].Value;
        if (!keys.Contains(key))
            failures.Add($"{position} reads the key '{key}', but the resource '{resource}' declares no such key.");
    }

    private static void CheckTyped(
        string file,
        string text,
        Match read,
        IReadOnlyDictionary<string, HashSet<string>> resourceKeys,
        List<string> failures)
    {
        string resource = read.Groups["localizer"].Value;
        string position = $"{ToRelativePath(file)}:{LineOf(text, read.Index)}";
        if (!resourceKeys.TryGetValue(resource, out HashSet<string>? keys))
        {
            failures.Add($"{position} uses the localization class '{resource}', which has no resource file.");
            return;
        }

        string key = read.Groups["key"].Value;
        if (!keys.Contains(key))
            failures.Add($"{position} reads the key '{key}', but the resource '{resource}' declares no such key.");
    }

    private static string ResourceName(string resourceBaseName)
    {
        const string root = "Revit.Linter.Localization.";
        string relative = resourceBaseName.StartsWith(root, StringComparison.Ordinal)
            ? resourceBaseName.Substring(root.Length)
            : resourceBaseName;
        return relative.Substring(relative.LastIndexOf('.') + 1);
    }

    private static string? ConstantString(TypeDeclarationSyntax type, string name) =>
        type.Members.OfType<FieldDeclarationSyntax>()
            .SelectMany(field => field.Declaration.Variables)
            .Where(variable => variable.Identifier.ValueText == name)
            .Select(variable => variable.Initializer?.Value)
            .OfType<LiteralExpressionSyntax>()
            .Select(literal => literal.Token.ValueText)
            .FirstOrDefault();

    private static IEnumerable<(string Root, string Expression, int Line)> BindingRoots(ViewFile view)
    {
        foreach (XElement element in view.Document.Root!.DescendantsAndSelf())
        {
            foreach (XAttribute attribute in element.Attributes())
            {
                if (attribute.Value.IndexOf("{Binding", StringComparison.Ordinal) < 0) continue;
                foreach (Match match in BindingExpression.Matches(attribute.Value))
                {
                    string? path = BindingPath(match.Groups["body"].Value);
                    if (path is null) continue;

                    string root = path.Split('.', '[', '/', '(', ' ')[0];
                    if (root.Length == 0 || !char.IsLetter(root[0]) && root[0] != '_') continue;
                    yield return (root, path, Line(attribute));
                }
            }
        }
    }

    private static string? BindingPath(string body)
    {
        // Markup extensions inside the binding carry their own commas and equals signs; flatten them first.
        string flattened = NestedMarkupExtension.Replace(body, "{}");
        if (SkippedBindingOptions.Any(option => flattened.IndexOf(option, StringComparison.Ordinal) >= 0))
            return null;

        string first = flattened.Split(',')[0].Trim();
        if (first.Length == 0) return null;

        int equals = first.IndexOf('=');
        if (equals >= 0)
        {
            if (!string.Equals(first.Substring(0, equals).Trim(), "Path", StringComparison.Ordinal)) return null;
            first = first.Substring(equals + 1).Trim();
        }

        return first.Length == 0 || first.StartsWith("{", StringComparison.Ordinal) ? null : first;
    }

    private static IReadOnlyList<ViewFile> LoadViews()
    {
        var views = new List<ViewFile>();
        foreach (string file in Directory.EnumerateFiles(Path.Combine(RepositoryRoot, "src"), "*.xaml", SearchOption.AllDirectories))
        {
            if (IsBuildOutput(file)) continue;
            if (ToRelativePath(file).IndexOf("/Views/", StringComparison.Ordinal) < 0) continue;

            XDocument document;
            try
            {
                document = XDocument.Load(file, LoadOptions.SetLineInfo);
            }
            catch (XmlException)
            {
                continue;
            }

            if (document.Root?.Attribute(Xaml + "Class") is null) continue;
            views.Add(new ViewFile(file, ProjectOf(file), document));
        }

        return views;
    }

    private static IReadOnlyDictionary<string, ProjectDesignData> LoadProjects()
    {
        var projects = new Dictionary<string, ProjectDesignData>(StringComparer.Ordinal);
        foreach (string designFile in DesignFiles())
        {
            string project = ProjectOf(designFile);
            if (!projects.TryGetValue(project, out ProjectDesignData? data))
            {
                data = new ProjectDesignData { Project = project };
                projects[project] = data;
            }

            SyntaxTree tree = CSharpSyntaxTree.ParseText(File.ReadAllText(designFile), path: designFile);
            var declared = new List<BaseTypeDeclarationSyntax>();
            foreach (TypeDeclarationSyntax type in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                data.Types[FullName(type)] = designFile;
                data.Members.UnionWith(MemberNames(type));
                declared.Add(type);
            }

            data.DesignSources.Add(string.Join(" ", declared.Select(type => type.ToFullString())));
        }

        foreach (ProjectDesignData data in projects.Values)
        {
            string designText = WithoutStringLiterals(string.Join(" ", data.DesignSources));
            var mentioned = new HashSet<string>(
                Word.Matches(designText).Cast<Match>().Select(match => match.Value),
                StringComparer.Ordinal);
            foreach (string file in ProjectFiles(data.Project, "*.cs"))
            {
                if (IsDesignFile(file)) continue;

                SyntaxTree tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file), path: file);
                foreach (TypeDeclarationSyntax type in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
                {
                    if (!mentioned.Contains(type.Identifier.ValueText)) continue;
                    data.Members.UnionWith(MemberNames(type));
                }
            }
        }

        return projects;
    }

    private static IReadOnlyDictionary<string, HashSet<string>> LoadResourceKeys()
    {
        var keys = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var sources = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (string file in Directory.EnumerateFiles(Path.Combine(RepositoryRoot, "src"), "*.resx", SearchOption.AllDirectories))
        {
            if (IsBuildOutput(file)) continue;

            string name = Path.GetFileNameWithoutExtension(file);
            if (name.IndexOf('.') >= 0) continue;

            sources[name] = (sources.TryGetValue(name, out int existing) ? existing : 0) + 1;
            keys[name] = new HashSet<string>(
                XDocument.Load(file).Root!.Elements("data")
                    .Select(element => element.Attribute("name")?.Value)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value!),
                StringComparer.Ordinal);
        }

        string[] ambiguous = sources.Where(pair => pair.Value > 1).Select(pair => pair.Key).ToArray();
        Assert.True(
            ambiguous.Length == 0,
            $"Several resource files declare the same class name: {string.Join(", ", ambiguous)}.");
        return keys;
    }

    private static IEnumerable<string> MemberNames(TypeDeclarationSyntax type)
    {
        foreach (MemberDeclarationSyntax member in type.Members)
        {
            switch (member)
            {
                case PropertyDeclarationSyntax property:
                    yield return property.Identifier.ValueText;
                    break;
                case FieldDeclarationSyntax field:
                    foreach (VariableDeclaratorSyntax variable in field.Declaration.Variables)
                        yield return variable.Identifier.ValueText;
                    break;
                case MethodDeclarationSyntax method when method.AttributeLists
                    .SelectMany(list => list.Attributes)
                    .Any(attribute => attribute.Name.ToString() is "RelayCommand" or "RelayCommandAttribute"):
                    string command = method.Identifier.ValueText;
                    if (command.EndsWith("Async", StringComparison.Ordinal))
                        command = command.Substring(0, command.Length - "Async".Length);
                    yield return command + "Command";
                    break;
            }
        }

        if (type is RecordDeclarationSyntax { ParameterList: { } parameters })
        {
            foreach (ParameterSyntax parameter in parameters.Parameters)
                yield return parameter.Identifier.ValueText;
        }
    }

    private static string FullName(TypeDeclarationSyntax type)
    {
        var names = new Stack<string>();
        for (SyntaxNode? node = type; node is not null; node = node.Parent)
        {
            switch (node)
            {
                case TypeDeclarationSyntax declaration:
                    names.Push(declaration.Identifier.ValueText);
                    break;
                case BaseNamespaceDeclarationSyntax declaration:
                    names.Push(declaration.Name.ToString());
                    break;
            }
        }

        return string.Join(".", names);
    }

    private static IEnumerable<string> DesignFiles() =>
        Directory.EnumerateFiles(Path.Combine(RepositoryRoot, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(file => !IsBuildOutput(file) && IsDesignFile(file));

    private static bool IsDesignFile(string file) =>
        Path.GetFileName(Path.GetDirectoryName(file)) == "DesignData";

    private static IEnumerable<string> ProjectFiles(string project, string pattern) =>
        Directory.EnumerateFiles(Path.Combine(RepositoryRoot, "src", project), pattern, SearchOption.AllDirectories)
            .Where(file => !IsBuildOutput(file));

    private static string ProjectOf(string file)
    {
        string[] segments = ToRelativePath(file).Split('/');
        return segments[1];
    }

    private static int Line(XObject node) =>
        node is IXmlLineInfo info && info.HasLineInfo() ? info.LineNumber : 0;

    private static int LineOf(string text, int offset)
    {
        int length = Math.Min(Math.Max(offset, 0), text.Length);
        return text.Substring(0, length).Count(character => character == '\n') + 1;
    }

    private static string ToRelativePath(string file)
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

    private static bool IsBuildOutput(string file) =>
        ToRelativePath(file).IndexOf("/bin/", StringComparison.Ordinal) >= 0
        || ToRelativePath(file).IndexOf("/obj/", StringComparison.Ordinal) >= 0;

    private static string WithoutStringLiterals(string content) => StringLiteral.Replace(content, "\"\"");

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

    private sealed record ViewFile(string Path, string Project, XDocument Document)
    {
        public string RelativePath => DesignDataTests.ToRelativePath(Path);
    }

    private sealed class ProjectDesignData
    {
        public string Project { get; init; } = string.Empty;

        public Dictionary<string, string> Types { get; } = new(StringComparer.Ordinal);

        public HashSet<string> Members { get; } = new(StringComparer.Ordinal);

        public List<string> DesignSources { get; } = [];
    }
}
