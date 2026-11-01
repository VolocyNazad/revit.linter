using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.Core.Abstractions.Services;

/// <summary>Provides the rules of every diagnostic configuration file in source precedence order.</summary>
public interface IDiagnosticConfigurationService
{
    /// <summary>Gets the top-level user configuration file path, where new built-in overrides are written.</summary>
    string ConfigurationPath { get; }

    /// <summary>
    /// Gets the valid rules of every readable configuration file, ordered by source precedence:
    /// the user file, the debug file, the installed examples and the managed practical-tour file.
    /// </summary>
    /// <returns>The valid rules. Invalid rules are reported separately and are not included.</returns>
    IReadOnlyList<DiagnosticRuleSource> GetRules();
}
