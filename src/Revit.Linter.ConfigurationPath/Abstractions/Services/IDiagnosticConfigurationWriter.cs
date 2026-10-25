using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.ConfigurationPath.Abstractions.Services;

/// <summary>Persists diagnostic state changes into the configuration file the diagnostic came from.</summary>
public interface IDiagnosticConfigurationWriter
{
    /// <summary>
    /// Writes the effective state of one diagnostic into the configuration file that declared it.
    /// </summary>
    /// <param name="path">The configuration file path reported by the diagnostic registration.</param>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="ruleType">The rule type of the diagnostic.</param>
    /// <param name="isActive">Whether the diagnostic participates in execution.</param>
    /// <param name="severity">The severity of the findings the diagnostic reports.</param>
    /// <param name="isHidden">Whether the diagnostic is hidden from the interface, execution and reports.</param>
    /// <remarks>
    /// A built-in override that does not exist yet is appended at the end of the rule list under the
    /// <c># Built-in overrides</c> marker. Comments, key order and the byte order mark are preserved.
    /// </remarks>
    void UpdateState(
        string path,
        string code,
        DiagnosticRuleType ruleType,
        bool isActive,
        DiagnosticSeverity severity,
        bool isHidden);

    /// <summary>
    /// Removes every built-in override entry and every <c>isHidden</c> field from the user-readable files.
    /// </summary>
    void ResetBuiltInOverrides();
}
