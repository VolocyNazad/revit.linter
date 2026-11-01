using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.Core.Abstractions.Services;

/// <summary>Supplies the configuration overrides declared for the built-in diagnostics.</summary>
public interface IBuiltInDiagnosticStateProvider
{
    /// <summary>Gets the user configuration file that holds the built-in overrides.</summary>
    string ConfigurationPath { get; }

    /// <summary>Gets the codes that have an explicit <c>builtIn</c> entry in the configuration.</summary>
    IReadOnlyList<string> OverriddenCodes { get; }

    /// <summary>
    /// Gets the effective state of a built-in diagnostic, falling back to the defaults supplied by its declaration.
    /// </summary>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="defaultSeverity">The severity declared in code.</param>
    /// <param name="defaultIsActive">Whether the diagnostic is enabled by default.</param>
    /// <returns>The state built from the first matching configuration entry, or from the defaults.</returns>
    DiagnosticState GetState(string code, DiagnosticSeverity defaultSeverity, bool defaultIsActive);
}
