using Revit.Linter.ConfigurationPath.Abstractions.Models;
using Revit.Linter.ConfigurationPath.Abstractions.Services;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.DiagnosticConfiguration.Editing;
using Revit.Linter.DiagnosticConfiguration.Infrastructure;
using Revit.Linter.DiagnosticConfiguration.Models;

namespace Revit.Linter.ConfigurationPath.Services;

/// <summary>
/// Writes diagnostic state changes into the YAML configuration without reformatting the rest of the file.
/// </summary>
/// <remarks>
/// The writer delegates the text edit to <see cref="DiagnosticConfigurationEditor"/>, which patches only the
/// scalar spans of the fields it changes. Comments, key order, quoting of untouched values and the byte order
/// mark survive, which matters because the configuration files are documented by hand. When a built-in
/// override does not exist yet, the writer appends it at the end of the rule list under the
/// <c># Built-in overrides</c> marker.
/// </remarks>
public sealed class DiagnosticConfigurationWriter : IDiagnosticConfigurationWriter
{
    private const string BuiltInOverridesMarker = "# Built-in overrides";
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
        bool isHidden) =>
        UpdateStates(
            path,
            [new DiagnosticRuleStateUpdate(code, ruleType, isActive, severity, isHidden)]);

    /// <inheritdoc />
    public void UpdateStates(string path, IReadOnlyCollection<DiagnosticRuleStateUpdate> updates)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("The path is required.", nameof(path));
        if (updates is null) throw new ArgumentNullException(nameof(updates));
        if (updates.Count == 0) return;
        if (updates.Any(update => string.IsNullOrWhiteSpace(update.Code)))
            throw new ArgumentException("The code is required.", nameof(updates));

        _configurationService.SuppressWatcherChanges(_watcherSuppression);
        try
        {
            if (!File.Exists(path))
            {
                if (updates.Any(update => update.RuleType != DiagnosticRuleType.BuiltIn))
                    throw new InvalidOperationException($"The configuration file '{path}' does not exist.");
                ConfigurationPathUtils.EnsureConfigurationFile(path);
            }

            string text = File.ReadAllText(path);
            DiagnosticConfigurationEditor editor = DiagnosticConfigurationEditor.Parse(text);
            List<DiagnosticRuleScalarUpdate> existing = [];
            List<DiagnosticRuleStateUpdate> appended = [];
            foreach (DiagnosticRuleStateUpdate update in updates)
            {
                var location = new DiagnosticRuleLocation(update.RuleType, update.Code);
                if (editor.ContainsRule(location))
                {
                    existing.Add(new DiagnosticRuleScalarUpdate(location, BuildStateFields(update)));
                }
                else if (update.RuleType == DiagnosticRuleType.BuiltIn)
                {
                    appended.Add(update);
                }
                else
                {
                    throw new InvalidOperationException(
                        $"The configuration file '{path}' does not contain the rule '{update.Code}'.");
                }
            }

            // One text edit pass for every existing rule, so a bulk action rewrites the file once. The
            // appended overrides form one block, which keeps their order and writes the marker once.
            string updated = existing.Count > 0 ? editor.SetScalars(existing) : text;
            if (appended.Count > 0)
            {
                updated = DiagnosticConfigurationEditor.Parse(updated).AppendRule(
                    string.Join(Environment.NewLine, appended.Select(BuildBuiltInEntry)),
                    BuiltInOverridesMarker);
            }

            if (string.Equals(updated, text, StringComparison.Ordinal)) return;

            // A span edit that would produce invalid YAML must never replace the user's file: the next
            // state write could not parse it, so every later check toggle would fail to persist. The
            // validation reuses the same parse path as the edit itself.
            DiagnosticConfigurationEditor.Parse(updated);

            DiagnosticConfigurationFileWriter.WriteAtomically(path, updated);
            _configurationService.Invalidate();
        }
        finally
        {
            _configurationService.SuppressWatcherChanges(_watcherSuppression);
        }
    }

    private static List<KeyValuePair<string, string>> BuildStateFields(DiagnosticRuleStateUpdate update) =>
    [
        new("isActive", update.IsActive ? "true" : "false"),
        new("severity", $"\"{update.Severity}\""),
        new("isHidden", update.IsHidden ? "true" : "false"),
    ];

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
                DiagnosticConfigurationEditor editor = DiagnosticConfigurationEditor.Parse(text);
                foreach (DiagnosticRuleLocation rule in editor.GetRules())
                {
                    if (rule.Type == DiagnosticRuleType.BuiltIn)
                        editor = DiagnosticConfigurationEditor.Parse(editor.RemoveRule(rule));
                    else
                        editor = DiagnosticConfigurationEditor.Parse(editor.RemoveField(rule, "isHidden"));
                }

                if (string.Equals(editor.Text, text, StringComparison.Ordinal)) continue;
                DiagnosticConfigurationFileWriter.WriteAtomically(file.Path, editor.Text);
            }

            _configurationService.Invalidate();
        }
        finally
        {
            _configurationService.SuppressWatcherChanges(_watcherSuppression);
        }
    }

    private static string BuildBuiltInEntry(DiagnosticRuleStateUpdate update) =>
        "- type: builtIn" + Environment.NewLine +
        $"  code: \"{update.Code}\"" + Environment.NewLine +
        $"  isActive: {(update.IsActive ? "true" : "false")}" + Environment.NewLine +
        $"  severity: \"{update.Severity}\"" + Environment.NewLine +
        $"  isHidden: {(update.IsHidden ? "true" : "false")}";
}
