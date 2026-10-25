using System.Text;
using Revit.Linter.ConfigurationPath.Abstractions.Models;
using Revit.Linter.ConfigurationPath.Abstractions.Services;
using Revit.Linter.ConfigurationPath.Services;
using Revit.Linter.Core.Abstractions.Models;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;

namespace Revit.Linter.ConfigurationPath.Services;

/// <summary>
/// Writes diagnostic state changes into the YAML configuration without reformatting the rest of the file.
/// </summary>
/// <remarks>
/// The writer patches only the scalar spans of the fields it changes. Comments, key order, quoting of
/// untouched values and the byte order mark survive, which matters because the configuration files are
/// documented by hand. When a built-in override does not exist yet, the writer appends it at the end of
/// the rule list under the <c># Built-in overrides</c> marker.
/// </remarks>
public sealed class DiagnosticConfigurationWriter : IDiagnosticConfigurationWriter
{
    private static readonly TimeSpan _watcherSuppression = TimeSpan.FromSeconds(3);
    private readonly DiagnosticConfigurationService _configurationService;

    /// <summary>Creates the writer over the shared configuration service.</summary>
    /// <param name="configurationService">The configuration service whose file set and change watcher are used.</param>
    public DiagnosticConfigurationWriter(DiagnosticConfigurationService configurationService)
        => _configurationService = configurationService;

    /// <inheritdoc />
    public void UpdateState(
        string path,
        string code,
        DiagnosticRuleType ruleType,
        bool isActive,
        DiagnosticSeverity severity,
        bool isHidden)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("The path is required.", nameof(path));
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("The code is required.", nameof(code));

        _configurationService.SuppressWatcherChanges(_watcherSuppression);
        try
        {
            if (!File.Exists(path))
            {
                if (ruleType != DiagnosticRuleType.BuiltIn)
                    throw new InvalidOperationException($"The configuration file '{path}' does not exist.");
                ConfigurationPathUtils.EnsureConfigurationFile(path);
            }

            string text = File.ReadAllText(path);
            string newLine = DetectNewLine(text);
            YamlSequenceNode? rules = LoadRules(text, out _);
            List<TextPatch> patches = [];
            YamlMappingNode? target = rules is null ? null : FindRule(rules, ruleType, code);
            if (target is not null)
            {
                AddFieldPatch(newLine, target, "isActive", isActive ? "true" : "false", patches);
                AddFieldPatch(newLine, target, "severity", $"\"{severity}\"", patches);
                AddFieldPatch(newLine, target, "isHidden", isHidden ? "true" : "false", patches);
            }
            else if (ruleType == DiagnosticRuleType.BuiltIn)
            {
                AppendBuiltInEntry(text, ruleType, code, isActive, severity, isHidden, patches);
            }
            else
            {
                throw new InvalidOperationException(
                    $"The configuration file '{path}' does not contain the rule '{code}'.");
            }

            if (patches.Count == 0) return;

            string updated = ApplyPatches(text, patches);
            WriteAtomically(path, updated);
            _configurationService.Invalidate();
        }
        finally
        {
            _configurationService.SuppressWatcherChanges(_watcherSuppression);
        }
    }

    /// <inheritdoc />
    public void ResetBuiltInOverrides()
    {
        _configurationService.SuppressWatcherChanges(_watcherSuppression);
        try
        {
            foreach (DiagnosticConfigurationFileDescriptor file in _configurationService.Files)
            {
                if (file.IsTour || !File.Exists(file.Path)) continue;

                string text = File.ReadAllText(file.Path);
                YamlSequenceNode? rules = LoadRules(text, out _);
                if (rules is null) continue;

                List<TextPatch> patches = [];
                foreach (YamlNode child in rules.Children.ToArray())
                {
                    if (child is not YamlMappingNode rule) continue;
                    if (IsBuiltInRule(rule))
                    {
                        AddRuleRemovalPatch(text, rule, patches);
                        continue;
                    }

                    YamlNode? hidden = FindValue(rule, "isHidden");
                    if (hidden is not null)
                        AddFieldRemovalPatch(text, rule, "isHidden", patches);
                }

                if (patches.Count == 0) continue;
                WriteAtomically(file.Path, ApplyPatches(text, patches));
            }

            _configurationService.Invalidate();
        }
        finally
        {
            _configurationService.SuppressWatcherChanges(_watcherSuppression);
        }
    }

    private static void AppendBuiltInEntry(
        string text,
        DiagnosticRuleType ruleType,
        string code,
        bool isActive,
        DiagnosticSeverity severity,
        bool isHidden,
        List<TextPatch> patches)
    {
        string newLine = DetectNewLine(text);
        YamlSequenceNode? rules = LoadRules(text, out _);
        int itemIndent = 2;
        if (rules is { Children.Count: > 0 } && rules.Children[0] is YamlMappingNode first)
        {
            int fieldIndent = FindKey(first, "type")?.Start.Column is long column ? (int)column - 1 : 2;
            itemIndent = Math.Max(0, fieldIndent - 2);
        }

        string entry =
            $"{new string(' ', itemIndent)}- type: {ruleType.ToConfigurationValue()}{newLine}" +
            $"{new string(' ', itemIndent + 2)}code: \"{code}\"{newLine}" +
            $"{new string(' ', itemIndent + 2)}isActive: {(isActive ? "true" : "false")}{newLine}" +
            $"{new string(' ', itemIndent + 2)}severity: \"{severity}\"{newLine}" +
            $"{new string(' ', itemIndent + 2)}isHidden: {(isHidden ? "true" : "false")}{newLine}";

        bool hasMarker = text.IndexOf("# Built-in overrides", StringComparison.Ordinal) >= 0;
        string prefix = hasMarker ? string.Empty : $"# Built-in overrides{newLine}";

        // An explicitly empty flow list, "rules: []", is replaced by the first block entry.
        if (rules is { Children.Count: 0 } && rules.Style == SequenceStyle.Flow)
        {
            int start = text.IndexOf('[', (int)rules.Start.Index);
            int end = start < 0 ? -1 : text.IndexOf(']', start);
            if (start >= 0 && end > start)
            {
                patches.Add(new TextPatch(start, end - start + 1, newLine + prefix + entry));
                return;
            }
        }

        if (rules is null)
            throw new InvalidOperationException("The configuration file has no rules list to append the built-in override to.");

        string separator = text.Length == 0 || text[^1] == '\n' ? string.Empty : newLine;
        patches.Add(new TextPatch(text.Length, 0, separator + prefix + entry));
    }

    private static void AddFieldPatch(
        string newLine,
        YamlMappingNode rule,
        string key,
        string value,
        List<TextPatch> patches)
    {
        YamlNode? existing = FindValue(rule, key);
        if (existing is not null)
        {
            patches.Add(new TextPatch(existing.Start.Index, existing.End.Index - existing.Start.Index, value));
            return;
        }

        int indent = FindKey(rule, "type")?.Start.Column is long column
            ? (int)column - 1
            : (int)rule.Start.Column - 1;
        YamlNode? last = rule.Children.Count == 0 ? null : rule.Children.Last().Value;
        long insertAt = last?.End.Index ?? rule.End.Index;
        patches.Add(new TextPatch(
            insertAt,
            0,
            $"{newLine}{new string(' ', indent)}{key}: {value}"));
    }

    private static void AddFieldRemovalPatch(
        string text,
        YamlMappingNode rule,
        string key,
        List<TextPatch> patches)
    {
        YamlNode? keyNode = FindKey(rule, key);
        YamlNode? value = FindValue(rule, key);
        if (keyNode is null || value is null) return;

        int lineStart = LineStart(text, keyNode.Start.Index);
        int lineEnd = LineEnd(text, value.End.Index);
        patches.Add(new TextPatch(lineStart, lineEnd - lineStart, string.Empty));
    }

    private static void AddRuleRemovalPatch(string text, YamlMappingNode rule, List<TextPatch> patches)
    {
        int lineStart = LineStart(text, rule.Start.Index);
        YamlNode? lastValue = rule.Children.Count == 0 ? null : rule.Children.Last().Value;
        int lineEnd = LineEnd(text, lastValue?.End.Index ?? rule.End.Index);
        patches.Add(new TextPatch(lineStart, lineEnd - lineStart, string.Empty));
    }

    private static int LineStart(string text, long index)
    {
        long adjusted = index - 1;
        int position;
        if (adjusted <= 0) position = 0;
        else if (adjusted >= text.Length) position = text.Length;
        else position = (int)adjusted;
        int lineStart = text.LastIndexOf('\n', Math.Max(0, position));
        return lineStart < 0 ? 0 : lineStart + 1;
    }

    private static int LineEnd(string text, long index)
    {
        int position;
        if (index <= 0) position = 0;
        else if (index >= text.Length) position = text.Length;
        else position = (int)index;
        int lineEnd = text.IndexOf('\n', Math.Min(text.Length, position));
        return lineEnd < 0 ? text.Length : lineEnd + 1;
    }

    private static YamlMappingNode? FindRule(YamlSequenceNode rules, DiagnosticRuleType ruleType, string code)
    {
        foreach (YamlNode child in rules.Children)
        {
            if (child is not YamlMappingNode rule) continue;
            if (!DiagnosticRuleTypes.TryParse(Scalar(rule, "type"), out DiagnosticRuleType type) || type != ruleType)
                continue;
            if (string.Equals(Scalar(rule, "code"), code, StringComparison.Ordinal))
                return rule;
        }

        return null;
    }

    private static bool IsBuiltInRule(YamlMappingNode rule) =>
        DiagnosticRuleTypes.TryParse(Scalar(rule, "type"), out DiagnosticRuleType type)
        && type == DiagnosticRuleType.BuiltIn;

    private static YamlNode? FindKey(YamlMappingNode mapping, string key) =>
        mapping.Children
            .Where(pair => pair.Key is YamlScalarNode scalar
                           && string.Equals(scalar.Value, key, StringComparison.OrdinalIgnoreCase))
            .Select(pair => pair.Key)
            .FirstOrDefault();

    private static YamlNode? FindValue(YamlMappingNode mapping, string key)
    {
        KeyValuePair<YamlNode, YamlNode> pair = mapping.Children
            .FirstOrDefault(pair => pair.Key is YamlScalarNode scalar
                                    && string.Equals(scalar.Value, key, StringComparison.OrdinalIgnoreCase));
        return pair.Key is null ? null : pair.Value;
    }

    private static string? Scalar(YamlMappingNode mapping, string key)
        => FindValue(mapping, key) is YamlScalarNode scalar ? scalar.Value : null;

    private static YamlSequenceNode? LoadRules(string text, out YamlMappingNode? root)
    {
        root = null;
        if (string.IsNullOrWhiteSpace(text)) return null;

        var stream = new YamlStream();
        stream.Load(new StringReader(text));
        if (stream.Documents.Count == 0 || stream.Documents[0].RootNode is not YamlMappingNode mapping)
            return null;

        root = mapping;
        return FindValue(mapping, "rules") as YamlSequenceNode;
    }

    private static string ApplyPatches(string text, List<TextPatch> patches)
    {
        StringBuilder result = new(text);
        foreach (TextPatch patch in patches.OrderByDescending(patch => patch.Start))
        {
            int start = (int)patch.Start;
            if (patch.Length == 0)
                result.Insert(start, patch.Replacement);
            else
                result.Remove(start, (int)patch.Length).Insert(start, patch.Replacement);
        }

        return result.ToString();
    }

    private static void WriteAtomically(string path, string text)
    {
        byte[] existing = File.Exists(path) ? File.ReadAllBytes(path) : [];
        bool hasBom = existing.Length >= 3
                      && existing[0] == 0xEF && existing[1] == 0xBB && existing[2] == 0xBF;
        string directory = Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException($"The configuration file path '{path}' has no directory.");
        Directory.CreateDirectory(directory);
        string temporaryPath = Path.Combine(directory, $"{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllText(temporaryPath, text, new UTF8Encoding(hasBom));
            if (File.Exists(path))
                File.Replace(temporaryPath, path, destinationBackupFileName: null);
            else
                File.Move(temporaryPath, path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // File.Replace is the atomic path; a restricted file system can refuse it. Falling back to a
            // direct write keeps the user's change, at the cost of a short window without atomicity.
            try
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
            catch (Exception cleanupException) when (cleanupException is IOException or UnauthorizedAccessException)
            {
                // The temporary file is left for the file system's cleanup; the change itself still lands.
            }

            File.WriteAllText(path, text, new UTF8Encoding(hasBom));
        }
    }

    private static string DetectNewLine(string text) =>
        text.IndexOf("\r\n", StringComparison.Ordinal) >= 0 ? "\r\n" : "\n";

    private readonly record struct TextPatch(long Start, long Length, string Replacement);
}
