using Microsoft.Extensions.Localization;
using Revit.Linter.ConfigurationPath.Abstractions.Services;

namespace Revit.Linter.Infrastructure.Services;

/// <summary>Localizes the stable rule schema problems reported by the shared configuration validator.</summary>
internal sealed class GlobalDiagnosticConfigurationRuleTextFormatter : IDiagnosticConfigurationRuleTextFormatter
{
    private readonly IStringLocalizer<GlobalLocalizations> _localizer;

    /// <summary>Initializes the formatter with the add-in global resources.</summary>
    /// <param name="localizer">Resolves the localized problem text.</param>
    public GlobalDiagnosticConfigurationRuleTextFormatter(IStringLocalizer<GlobalLocalizations> localizer)
        => _localizer = localizer;

    /// <inheritdoc />
    public string Format(string issueCode, IReadOnlyList<string> arguments) => issueCode switch
    {
        "UnknownType" => _localizer["diagnosticConfiguration_ruleUnknownType_message", arguments[0]],
        "MissingCode" => _localizer["diagnosticConfiguration_ruleMissingCode_message"],
        "MissingField" => _localizer["diagnosticConfiguration_ruleMissingField_message", arguments[0]],
        "MissingParameters" => _localizer["diagnosticConfiguration_ruleMissingParameters_message"],
        "UnsupportedField" => _localizer[
            "diagnosticConfiguration_ruleUnsupportedField_message", arguments[0], arguments[1]],
        "UnsupportedType" => _localizer["diagnosticConfiguration_ruleUnsupportedType_message", arguments[0]],
        _ => _localizer["diagnosticConfiguration_ruleUnknown_message", issueCode],
    };
}
