using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ConfigurationPath.Services;

/// <summary>Applies the <c>builtIn</c> configuration entries to the diagnostics that ship with the product.</summary>
public sealed class BuiltInDiagnosticStateProvider : IBuiltInDiagnosticStateProvider
{
    private readonly IDiagnosticConfigurationService _configurationService;

    /// <summary>Creates the provider over the shared configuration source.</summary>
    /// <param name="configurationService">The configuration source that contains the built-in entries.</param>
    public BuiltInDiagnosticStateProvider(IDiagnosticConfigurationService configurationService)
        => _configurationService = configurationService;

    /// <inheritdoc />
    public string ConfigurationPath => _configurationService.ConfigurationPath;

    /// <inheritdoc />
    public IReadOnlyList<string> OverriddenCodes => _configurationService.GetRules()
        .Select(source => source.Rule)
        .Where(IsBuiltIn)
        .Select(rule => rule.Code)
        .Where(code => !string.IsNullOrWhiteSpace(code))
        .Select(code => code!)
        .Distinct(StringComparer.Ordinal)
        .ToList()
        .AsReadOnly();

    /// <inheritdoc />
    public DiagnosticState GetState(string code, DiagnosticSeverity defaultSeverity, bool defaultIsActive)
    {
        DiagnosticRule? rule = _configurationService.GetRules()
            .Select(source => source.Rule)
            .Where(IsBuiltIn)
            .FirstOrDefault(candidate => string.Equals(candidate.Code, code, StringComparison.Ordinal));
        return rule is null
            ? new DiagnosticState(defaultIsActive, defaultSeverity, isHidden: false)
            : new DiagnosticState(
                rule.IsActive ?? defaultIsActive,
                rule.Severity ?? defaultSeverity,
                rule.IsHidden ?? false);
    }

    private static bool IsBuiltIn(DiagnosticRule rule) =>
        DiagnosticRuleTypes.TryParse(rule.Type, out DiagnosticRuleType type)
        && type == DiagnosticRuleType.BuiltIn;
}
