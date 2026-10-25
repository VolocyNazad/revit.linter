namespace Revit.Linter.ConfigurationPath.Abstractions.Services;

/// <summary>Formats a rule schema problem for the user interface.</summary>
/// <remarks>
/// The shared validator reports stable codes and arguments; the host supplies the localized text.
/// A host that has no resources can leave the formatter unregistered and receive the validator's
/// English description instead.
/// </remarks>
public interface IDiagnosticConfigurationRuleTextFormatter
{
    /// <summary>Formats one rule problem.</summary>
    /// <param name="issueCode">The stable name of the validator issue code, such as <c>MissingField</c>.</param>
    /// <param name="arguments">The issue arguments in the order the validator produced them.</param>
    /// <returns>The user-facing description of the problem.</returns>
    string Format(string issueCode, IReadOnlyList<string> arguments);
}
