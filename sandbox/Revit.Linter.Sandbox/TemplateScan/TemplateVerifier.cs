using Autodesk.Revit.DB;

namespace Revit.Linter.Sandbox.TemplateScan;

/// <summary>One parameter expected by a generated check fragment.</summary>
internal sealed class ExpectedParameter
{
    public string Name { get; set; } = string.Empty;

    public string? Guid { get; set; }

    public string Group { get; set; } = string.Empty;

    public bool IsInstance { get; set; }

    public bool AllowVaryBetweenGroups { get; set; }

    public List<string> Categories { get; } = [];
}

/// <summary>
/// Verifies a generated check fragment against the template document it was generated from: the
/// expectation is that the document satisfies its own check with no findings.
/// </summary>
internal static class TemplateVerifier
{
    private static readonly Dictionary<string, string> PackFileNames = new(StringComparer.Ordinal)
    {
        ["АР"] = "adsk-401.yaml",
        ["ВК"] = "adsk-402.yaml",
        ["КЖ"] = "adsk-403.yaml",
        ["КМ"] = "adsk-404.yaml",
        ["ОВ"] = "adsk-405.yaml",
        ["СС"] = "adsk-406.yaml",
        ["ТХ"] = "adsk-407.yaml",
        ["ЭОМ"] = "adsk-408.yaml",
    };

    public static void Verify(Document document, TemplateSnapshot snapshot, string packsDirectory)
    {
        if (!PackFileNames.TryGetValue(snapshot.Discipline, out string? fileName))
        {
            snapshot.Verification = ["unknown discipline: " + snapshot.Discipline];
            return;
        }

        string path = Path.Combine(packsDirectory, fileName);
        snapshot.VerificationFile = path;
        if (!File.Exists(path))
        {
            snapshot.Verification = ["check fragment not found: " + path];
            return;
        }

        List<ExpectedParameter> expected;
        try
        {
            expected = ReadFragment(path);
        }
        catch (Exception exception)
        {
            snapshot.Verification = ["fragment parse failure: " + exception.GetType().Name + ": " + exception.Message];
            return;
        }

        snapshot.VerificationChecked = expected.Count;
        try
        {
            snapshot.Verification = Compare(document, expected);
        }
        catch (Exception exception)
        {
            snapshot.Verification = ["comparison failure: " + exception.GetType().Name + ": " + exception.Message];
        }
    }

    private static List<ExpectedParameter> ReadFragment(string path)
    {
        List<ExpectedParameter> result = [];
        ExpectedParameter? current = null;
        foreach (string rawLine in File.ReadAllLines(path))
        {
            string line = rawLine.TrimEnd();
            if (line.StartsWith("    - name: ", StringComparison.Ordinal))
            {
                current = new ExpectedParameter { Name = Unquote(line.Substring("    - name: ".Length)) };
                result.Add(current);
                continue;
            }

            if (current is null)
                continue;

            if (line.StartsWith("      guid: ", StringComparison.Ordinal))
                current.Guid = Unquote(line.Substring("      guid: ".Length));
            else if (line.StartsWith("      group: ", StringComparison.Ordinal))
                current.Group = Unquote(line.Substring("      group: ".Length));
            else if (line.StartsWith("      isInstance: ", StringComparison.Ordinal))
                current.IsInstance = bool.Parse(line.Substring("      isInstance: ".Length).Trim());
            else if (line.StartsWith("      allowVaryBetweenGroups: ", StringComparison.Ordinal))
                current.AllowVaryBetweenGroups =
                    bool.Parse(line.Substring("      allowVaryBetweenGroups: ".Length).Trim());
            else if (line.StartsWith("      categories: ", StringComparison.Ordinal))
                current.Categories.AddRange(ParseCategories(line.Substring("      categories: ".Length)));
        }

        return result;
    }

    private static string Unquote(string value)
    {
        string text = value.Trim();
        if (text.Length >= 2 && text[0] == '"' && text[text.Length - 1] == '"')
            text = text.Substring(1, text.Length - 2);
        return text.Replace("\\\"", "\"").Replace("\\\\", "\\");
    }

    private static IEnumerable<string> ParseCategories(string value)
    {
        string text = value.Trim();
        int start = text.IndexOf('[');
        int end = text.LastIndexOf(']');
        if (start < 0 || end <= start)
            yield break;

        foreach (string part in text.Substring(start + 1, end - start - 1).Split(','))
        {
            string category = Unquote(part.Trim());
            if (category.Length > 0)
                yield return category;
        }
    }

    private static List<string> Compare(Document document, IReadOnlyList<ExpectedParameter> expected)
    {
        List<string> problems = [];
        Dictionary<string, ParameterElement> byName = new(StringComparer.Ordinal);
        foreach (ParameterElement element in new FilteredElementCollector(document)
                     .OfClass(typeof(ParameterElement))
                     .Cast<ParameterElement>())
        {
            if (!byName.ContainsKey(element.Name))
                byName[element.Name] = element;
        }

        foreach (ExpectedParameter parameter in expected)
        {
            if (!Enum.IsDefined(typeof(BuiltInParameterGroup), parameter.Group))
                problems.Add($"{parameter.Name}: unknown parameter group '{parameter.Group}'");
            foreach (string category in parameter.Categories)
            {
                if (!Enum.IsDefined(typeof(BuiltInCategory), category))
                    problems.Add($"{parameter.Name}: unknown category '{category}'");
            }

            ParameterElement? element;
            if (!string.IsNullOrEmpty(parameter.Guid))
            {
                if (!Guid.TryParse(parameter.Guid, out Guid guid))
                {
                    problems.Add($"{parameter.Name}: invalid guid '{parameter.Guid}'");
                    continue;
                }

                element = SharedParameterElement.Lookup(document, guid);
                if (element is null)
                {
                    problems.Add($"{parameter.Name}: shared parameter '{parameter.Guid}' not found");
                    continue;
                }
            }
            else if (!byName.TryGetValue(parameter.Name, out element))
            {
                problems.Add($"{parameter.Name}: parameter not found");
                continue;
            }

            if (element.GetDefinition() is not InternalDefinition definition)
            {
                problems.Add($"{parameter.Name}: definition is not an internal definition");
                continue;
            }

            if (!string.Equals(definition.Name, parameter.Name, StringComparison.Ordinal))
                problems.Add($"{parameter.Name}: name is '{definition.Name}'");
            if (!(document.ParameterBindings.get_Item(definition) is ElementBinding binding))
            {
                problems.Add($"{parameter.Name}: not bound to categories");
                continue;
            }

            if (!string.Equals(definition.ParameterGroup.ToString(), parameter.Group, StringComparison.Ordinal))
                problems.Add($"{parameter.Name}: group is '{definition.ParameterGroup}'");
            bool isInstance = binding is InstanceBinding;
            if (isInstance != parameter.IsInstance)
                problems.Add($"{parameter.Name}: binding type differs ({(isInstance ? "instance" : "type")})");
            if (definition.VariesAcrossGroups != parameter.AllowVaryBetweenGroups)
                problems.Add($"{parameter.Name}: varies across groups is {definition.VariesAcrossGroups}");

            HashSet<string> actual = new(
                binding.Categories.Cast<Category>().Select(category => ((BuiltInCategory)category.Id.IntegerValue).ToString()),
                StringComparer.Ordinal);
            HashSet<string> wanted = new(parameter.Categories, StringComparer.Ordinal);
            if (!actual.SetEquals(wanted))
            {
                problems.Add(
                    $"{parameter.Name}: categories differ (document {actual.Count}, expected {wanted.Count}; " +
                    $"extra: {string.Join(", ", actual.Except(wanted).OrderBy(name => name))}; " +
                    $"missing: {string.Join(", ", wanted.Except(actual).OrderBy(name => name))})");
            }
        }

        return problems;
    }
}
