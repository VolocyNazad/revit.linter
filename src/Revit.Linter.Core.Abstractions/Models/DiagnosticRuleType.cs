namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>Identifies the module that owns a diagnostic rule and how the rule is applied.</summary>
public enum DiagnosticRuleType
{
    /// <summary>A user-defined element diagnostic loaded from a configuration file.</summary>
    Custom,
    /// <summary>A collision element diagnostic loaded from a configuration file.</summary>
    Collision,
    /// <summary>A project-parameter document diagnostic loaded from a configuration file.</summary>
    Parameter,
    /// <summary>An override entry for a diagnostic that ships with the product.</summary>
    BuiltIn,
}

/// <summary>Converts diagnostic rule types to and from their configuration file spelling.</summary>
public static class DiagnosticRuleTypes
{
    /// <summary>Gets the spelling of the rule type used in configuration files.</summary>
    /// <param name="type">The rule type.</param>
    /// <returns>The lower-case configuration value, for example <c>collision</c>.</returns>
    public static string ToConfigurationValue(this DiagnosticRuleType type) => type switch
    {
        DiagnosticRuleType.Custom => "custom",
        DiagnosticRuleType.Collision => "collision",
        DiagnosticRuleType.Parameter => "parameter",
        DiagnosticRuleType.BuiltIn => "builtIn",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown diagnostic rule type."),
    };

    /// <summary>Parses the configuration spelling of a rule type, ignoring case.</summary>
    /// <param name="value">The configuration value.</param>
    /// <param name="type">The parsed rule type.</param>
    /// <returns><see langword="true"/> when the value names a known rule type; otherwise, <see langword="false"/>.</returns>
    public static bool TryParse(string? value, out DiagnosticRuleType type)
    {
        type = default;
        if (string.IsNullOrWhiteSpace(value)) return false;

        string trimmed = value!.Trim();
        if (string.Equals(trimmed, "custom", StringComparison.OrdinalIgnoreCase))
        {
            type = DiagnosticRuleType.Custom;
            return true;
        }

        if (string.Equals(trimmed, "collision", StringComparison.OrdinalIgnoreCase))
        {
            type = DiagnosticRuleType.Collision;
            return true;
        }

        if (string.Equals(trimmed, "parameter", StringComparison.OrdinalIgnoreCase))
        {
            type = DiagnosticRuleType.Parameter;
            return true;
        }

        if (string.Equals(trimmed, "builtIn", StringComparison.OrdinalIgnoreCase))
        {
            type = DiagnosticRuleType.BuiltIn;
            return true;
        }

        return false;
    }
}
