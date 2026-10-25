using System.Text;
using Revit.Linter.Core.Abstractions.Models;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;

namespace Revit.Linter.DiagnosticConfiguration.Editing;

/// <summary>Identifies one rule inside a diagnostic configuration document.</summary>
/// <param name="Type">The rule type discriminator.</param>
/// <param name="Code">The stable diagnostic code as written in the file.</param>
public readonly record struct DiagnosticRuleLocation(DiagnosticRuleType Type, string Code);

/// <summary>
/// Applies minimal text edits to one diagnostic configuration document.
/// </summary>
/// <remarks>
/// The editor never regenerates the whole file from the parsed model: it locates the affected YAML
/// nodes in the original text and replaces only their spans. Comments, key order, the quoting of
/// untouched values, the byte order mark and the newline style therefore survive an edit, which matters
/// because the configuration files are documented by hand. A caller that cannot express an edit this
/// way must fall back to editing the file as text instead of rebuilding it.
/// </remarks>
public sealed class DiagnosticConfigurationEditor
{
    private const string RuleListKey = "rules";
    private const string RuleTypeKey = "type";

    private readonly YamlSequenceNode? _rules;

    private DiagnosticConfigurationEditor(string text)
    {
        Text = text;
        NewLine = text.IndexOf("\r\n", StringComparison.Ordinal) >= 0 ? "\r\n" : "\n";

        var stream = new YamlStream();
        stream.Load(new StringReader(text));
        if (stream.Documents.Count > 0 && stream.Documents[0].RootNode is YamlMappingNode root)
            _rules = FindValue(root, RuleListKey) as YamlSequenceNode;
    }

    /// <summary>Gets the current document text.</summary>
    public string Text { get; }

    private string NewLine { get; }

    /// <summary>Parses configuration text for editing.</summary>
    /// <param name="text">The complete configuration file content.</param>
    /// <returns>The editor over the unchanged text.</returns>
    /// <exception cref="YamlException">The text is not valid YAML.</exception>
    public static DiagnosticConfigurationEditor Parse(string text)
    {
        if (text is null) throw new ArgumentNullException(nameof(text));
        return new DiagnosticConfigurationEditor(text);
    }

    /// <summary>Determines whether the document contains the rule.</summary>
    /// <param name="location">The rule to find.</param>
    /// <returns><see langword="true"/> when a rule with the same type and code exists.</returns>
    public bool ContainsRule(DiagnosticRuleLocation location) => FindRule(location) is not null;

    /// <summary>Gets the rules of the document in file order.</summary>
    /// <returns>
    /// The locations of the rules whose type discriminator is known. A rule without a code is returned
    /// with an empty code so a caller that removes rules by provenance still sees it.
    /// </returns>
    public IReadOnlyList<DiagnosticRuleLocation> GetRules()
    {
        if (_rules is null) return [];

        List<DiagnosticRuleLocation> rules = [];
        foreach (YamlNode child in _rules.Children)
        {
            if (child is not YamlMappingNode rule) continue;
            if (!DiagnosticRuleTypes.TryParse(Scalar(rule, RuleTypeKey), out DiagnosticRuleType type))
                continue;
            rules.Add(new DiagnosticRuleLocation(type, Scalar(rule, "code") ?? string.Empty));
        }

        return rules;
    }

    /// <summary>Replaces the value of one existing or missing rule field.</summary>
    /// <param name="location">The rule to edit.</param>
    /// <param name="key">The field key as written in the file.</param>
    /// <param name="yamlValue">The replacement value exactly as YAML text, for example <c>"Warning"</c> or <c>false</c>.</param>
    /// <returns>The edited document text.</returns>
    /// <exception cref="InvalidOperationException">The rule is not present in the document.</exception>
    public string SetScalar(DiagnosticRuleLocation location, string key, string yamlValue) =>
        SetScalars(location, [new KeyValuePair<string, string>(key, yamlValue)]);

    /// <summary>Replaces the values of the given rule fields in one edit pass.</summary>
    /// <param name="location">The rule to edit.</param>
    /// <param name="values">The field keys and their replacement values exactly as YAML text.</param>
    /// <returns>The edited document text.</returns>
    /// <exception cref="InvalidOperationException">The rule is not present in the document.</exception>
    /// <remarks>
    /// A field that exists in the document is replaced in place; a missing field is inserted after the
    /// last field of the rule, using the indentation of the rule's existing fields. Comments and the
    /// order of the other fields are untouched.
    /// </remarks>
    public string SetScalars(
        DiagnosticRuleLocation location,
        IReadOnlyList<KeyValuePair<string, string>> values)
    {
        if (values.Count == 0) return Text;

        YamlMappingNode rule = FindRequiredRule(location);
        List<TextPatch> patches = [];
        List<KeyValuePair<string, string>> missing = [];

        foreach (KeyValuePair<string, string> pair in values)
        {
            YamlNode? existing = FindValue(rule, pair.Key);
            if (existing is not null)
            {
                patches.Add(new TextPatch(
                    existing.Start.Index, existing.End.Index - existing.Start.Index, pair.Value));
                continue;
            }

            missing.Add(pair);
        }

        if (missing.Count > 0)
        {
            int indent = FieldIndent(rule);
            YamlNode? last = rule.Children.Count == 0 ? null : rule.Children.Last().Value;
            long insertAt = last?.End.Index ?? rule.End.Index;
            var builder = new StringBuilder();
            foreach (KeyValuePair<string, string> pair in missing)
                builder.Append(NewLine).Append(' ', indent).Append(pair.Key).Append(": ").Append(pair.Value);
            patches.Add(new TextPatch(insertAt, 0, builder.ToString()));
        }

        return ApplyPatches(patches);
    }

    /// <summary>Removes one field from the rule.</summary>
    /// <param name="location">The rule to edit.</param>
    /// <param name="key">The field key as written in the file.</param>
    /// <returns>The edited document text; unchanged when the rule or the field does not exist.</returns>
    public string RemoveField(DiagnosticRuleLocation location, string key)
    {
        if (FindRule(location) is not { } rule) return Text;
        YamlNode? keyNode = FindKey(rule, key);
        YamlNode? value = FindValue(rule, key);
        if (keyNode is null || value is null) return Text;

        int lineStart = LineStart(keyNode.Start.Index);
        int lineEnd = LineEnd(value.End.Index);
        return ApplyPatches([new TextPatch(lineStart, lineEnd - lineStart, string.Empty)]);
    }

    /// <summary>Removes the rule together with its fields.</summary>
    /// <param name="location">The rule to remove.</param>
    /// <returns>The edited document text; unchanged when the rule does not exist.</returns>
    /// <exception cref="InvalidOperationException">The document has no rule list.</exception>
    public string RemoveRule(DiagnosticRuleLocation location)
    {
        if (_rules is null) return Text;
        YamlMappingNode? rule = FindRule(location);
        if (rule is null) return Text;

        int lineStart = LineStart(rule.Start.Index);
        YamlNode? lastValue = rule.Children.Count == 0 ? null : rule.Children.Last().Value;
        int lineEnd = LineEnd(lastValue?.End.Index ?? rule.End.Index);
        return ApplyPatches([new TextPatch(lineStart, lineEnd - lineStart, string.Empty)]);
    }

    /// <summary>Appends a new rule block to the end of the rule list.</summary>
    /// <param name="ruleYaml">
    /// The rule block, normally starting with <c>- type: ...</c>. Any common indentation is normalized to
    /// the document's existing rule indentation; the first line is the rule item itself.
    /// </param>
    /// <param name="markerComment">
    /// An optional comment line to write before the first appended rule when that comment is not yet
    /// present in the document, for example <c># Built-in overrides</c>.
    /// </param>
    /// <returns>The edited document text.</returns>
    /// <exception cref="InvalidOperationException">The document has no rule list.</exception>
    /// <exception cref="ArgumentException">The block is empty.</exception>
    public string AppendRule(string ruleYaml, string? markerComment = null)
    {
        if (_rules is null)
            throw new InvalidOperationException("The configuration text has no rules list to append the rule to.");

        string block = NormalizeBlock(ruleYaml, NewLine);
        string entry = IndentBlock(block, ItemIndent(), NewLine);
        string prefix = markerComment is null || Text.Contains(markerComment)
            ? string.Empty
            : markerComment + NewLine;
        List<TextPatch> patches = [];

        // An explicitly empty flow list, "rules: []", is replaced by the first block entry.
        if (_rules.Children.Count == 0 && _rules.Style == SequenceStyle.Flow)
        {
            int start = Text.IndexOf('[', (int)_rules.Start.Index);
            int end = start < 0 ? -1 : Text.IndexOf(']', start);
            if (start >= 0 && end > start)
            {
                patches.Add(new TextPatch(start, end - start + 1, NewLine + prefix + entry));
                return ApplyPatches(patches);
            }
        }

        string separator = Text.Length == 0 || Text[Text.Length - 1] == '\n' ? string.Empty : NewLine;
        patches.Add(new TextPatch(Text.Length, 0, separator + prefix + entry));
        return ApplyPatches(patches);
    }

    private YamlMappingNode FindRequiredRule(DiagnosticRuleLocation location)
        => FindRule(location)
           ?? throw new InvalidOperationException(
               $"The configuration text does not contain a '{location.Type.ToConfigurationValue()}' rule with the code '{location.Code}'.");

    private YamlMappingNode? FindRule(DiagnosticRuleLocation location)
    {
        if (_rules is null) return null;

        foreach (YamlNode child in _rules.Children)
        {
            if (child is not YamlMappingNode rule) continue;
            if (!DiagnosticRuleTypes.TryParse(Scalar(rule, RuleTypeKey), out DiagnosticRuleType type)
                || type != location.Type)
                continue;
            if (string.Equals(Scalar(rule, "code"), location.Code, StringComparison.Ordinal))
                return rule;
        }

        return null;
    }

    private int ItemIndent()
    {
        int itemIndent = 2;
        if (_rules is { Children.Count: > 0 } && _rules.Children[0] is YamlMappingNode first)
            itemIndent = Math.Max(0, FieldIndent(first) - 2);
        return itemIndent;
    }

    private static int FieldIndent(YamlMappingNode rule) =>
        FindKey(rule, RuleTypeKey)?.Start.Column is long column
            ? (int)column - 1
            : (int)rule.Start.Column - 1;

    private string ApplyPatches(List<TextPatch> patches)
    {
        if (patches.Count == 0) return Text;

        var result = new StringBuilder(Text);
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

    private int LineStart(long index)
    {
        long adjusted = index - 1;
        int position;
        if (adjusted <= 0) position = 0;
        else if (adjusted >= Text.Length) position = Text.Length;
        else position = (int)adjusted;
        int lineStart = Text.LastIndexOf('\n', Math.Max(0, position));
        return lineStart < 0 ? 0 : lineStart + 1;
    }

    private int LineEnd(long index)
    {
        int position;
        if (index <= 0) position = 0;
        else if (index >= Text.Length) position = Text.Length;
        else position = (int)index;
        int lineEnd = Text.IndexOf('\n', Math.Min(Text.Length, position));
        return lineEnd < 0 ? Text.Length : lineEnd + 1;
    }

    private static string NormalizeBlock(string yaml, string newLine)
    {
        if (yaml is null) throw new ArgumentNullException(nameof(yaml));
        string normalized = yaml.Replace("\r\n", "\n").Replace('\r', '\n');
        string[] lines = normalized.Split('\n');
        int start = 0;
        int end = lines.Length - 1;
        while (start <= end && string.IsNullOrWhiteSpace(lines[start])) start++;
        while (end >= start && string.IsNullOrWhiteSpace(lines[end])) end--;
        if (start > end) throw new ArgumentException("The rule block is empty.", nameof(yaml));

        int commonIndent = int.MaxValue;
        for (int index = start; index <= end; index++)
        {
            string line = lines[index];
            if (line.Length == 0) continue;
            int indent = 0;
            while (indent < line.Length && line[indent] == ' ') indent++;
            commonIndent = Math.Min(commonIndent, indent);
        }

        var builder = new StringBuilder();
        for (int index = start; index <= end; index++)
        {
            string line = lines[index];
            string content = line.Length >= commonIndent ? line.Substring(commonIndent) : line.TrimStart();
            builder.Append(content).Append(newLine);
        }

        return builder.ToString();
    }

    private static string IndentBlock(string block, int indent, string newLine)
    {
        string prefix = new string(' ', indent);
        var builder = new StringBuilder();
        foreach (string line in block.Split(new[] { newLine }, StringSplitOptions.None))
        {
            if (line.Length == 0) continue;
            builder.Append(prefix).Append(line).Append(newLine);
        }

        return builder.ToString();
    }

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

    private readonly record struct TextPatch(long Start, long Length, string Replacement);
}
