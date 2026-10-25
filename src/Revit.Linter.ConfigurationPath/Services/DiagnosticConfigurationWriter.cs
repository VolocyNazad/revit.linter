using Revit.Linter.ConfigurationPath.Abstractions.Services;
using Revit.Linter.ConfigurationPath.Services;
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
            DiagnosticConfigurationEditor editor = DiagnosticConfigurationEditor.Parse(text);
            var location = new DiagnosticRuleLocation(ruleType, code);
            string updated;
            if (editor.ContainsRule(location))
            {
                updated = editor.SetScalars(
                    location,
                    [
                        new("isActive", isActive ? "true" : "false"),
                        new("severity", $"\"{severity}\""),
                        new("isHidden", isHidden ? "true" : "false"),
                    ]);
            }
            else if (ruleType == DiagnosticRuleType.BuiltIn)
            {
                updated = editor.AppendRule(
                    BuildBuiltInEntry(code, isActive, severity, isHidden), BuiltInOverridesMarker);
            }
            else
            {
                throw new InvalidOperationException(
                    $"The configuration file '{path}' does not contain the rule '{code}'.");
            }

            if (string.Equals(updated, text, StringComparison.Ordinal)) return;

            DiagnosticConfigurationFileWriter.WriteAtomically(path, updated);
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

    private static string BuildBuiltInEntry(
        string code,
        bool isActive,
        DiagnosticSeverity severity,
        bool isHidden) =>
        "- type: builtIn" + Environment.NewLine +
        $"  code: \"{code}\"" + Environment.NewLine +
        $"  isActive: {(isActive ? "true" : "false")}" + Environment.NewLine +
        $"  severity: \"{severity}\"" + Environment.NewLine +
        $"  isHidden: {(isHidden ? "true" : "false")}";
}
