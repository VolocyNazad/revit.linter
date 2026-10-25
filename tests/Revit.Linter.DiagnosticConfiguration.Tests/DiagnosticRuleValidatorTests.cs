using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.DiagnosticConfiguration.Validation;

namespace Revit.Linter.DiagnosticConfiguration.Tests;

public sealed class DiagnosticRuleValidatorTests
{
    [Fact]
    public void A_complete_custom_rule_is_valid()
    {
        DiagnosticRule rule = CustomRule(
            takeDocument: "!property('IsFamilyDocument')", take: "instance", check: "true");

        Assert.Empty(DiagnosticRuleValidator.Validate(rule));
    }

    [Theory]
    [InlineData("takeDocument")]
    [InlineData("take")]
    [InlineData("check")]
    public void A_custom_rule_requires_each_formula(string missingField)
    {
        DiagnosticRule rule = missingField switch
        {
            "takeDocument" => CustomRule(takeDocument: null, take: "instance", check: "true"),
            "take" => CustomRule(takeDocument: "!property('IsFamilyDocument')", take: null, check: "true"),
            _ => CustomRule(takeDocument: "!property('IsFamilyDocument')", take: "instance", check: null),
        };

        IReadOnlyList<string> errors = DiagnosticRuleValidator.Validate(rule);

        Assert.Contains(errors, error => error.Contains(missingField, StringComparison.Ordinal));
    }

    [Fact]
    public void An_unknown_type_is_rejected()
    {
        var rule = new DiagnosticRule { Type = "mystery", Code = "X001" };

        IReadOnlyList<string> errors = DiagnosticRuleValidator.Validate(rule);

        Assert.Contains(errors, error => error.Contains("unknown type", StringComparison.Ordinal));
    }

    [Fact]
    public void A_missing_code_is_rejected()
    {
        var rule = new DiagnosticRule
        {
            Type = "custom",
            TakeDocument = "!property('IsFamilyDocument')",
            Take = "instance",
            Check = "true",
        };

        Assert.Contains(DiagnosticRuleValidator.Validate(rule),
            error => error.Contains("code", StringComparison.Ordinal));
    }

    [Fact]
    public void A_complete_collision_rule_is_valid()
    {
        var rule = new DiagnosticRule
        {
            Type = "collision",
            Code = "CLSN001",
            TakeDocument = "!property('IsFamilyDocument')",
            Take = "instance",
            AndTake = "instance",
            GroupBy = "'all'",
        };

        Assert.Empty(DiagnosticRuleValidator.Validate(rule));
    }

    [Fact]
    public void A_collision_rule_requires_group_by_and_and_take()
    {
        var rule = new DiagnosticRule
        {
            Type = "collision",
            Code = "CLSN001",
            TakeDocument = "!property('IsFamilyDocument')",
            Take = "instance",
        };

        IReadOnlyList<string> errors = DiagnosticRuleValidator.Validate(rule);

        Assert.Contains(errors, error => error.Contains("andTake", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("groupBy", StringComparison.Ordinal));
    }

    [Fact]
    public void A_parameter_rule_requires_parameters()
    {
        var rule = new DiagnosticRule { Type = "parameter", Code = "PARAM001", Take = "instance" };

        Assert.Contains(DiagnosticRuleValidator.Validate(rule),
            error => error.Contains("parameters", StringComparison.Ordinal));
    }

    [Fact]
    public void A_built_in_override_accepts_only_state_fields()
    {
        var rule = new DiagnosticRule
        {
            Type = "builtIn",
            Code = "SHRD003",
            IsActive = false,
            Severity = DiagnosticSeverity.Warning,
            IsHidden = true,
        };

        Assert.Empty(DiagnosticRuleValidator.Validate(rule));
    }

    [Fact]
    public void A_built_in_override_rejects_definition_fields()
    {
        var rule = new DiagnosticRule
        {
            Type = "builtIn",
            Code = "SHRD003",
            Take = "instance",
        };

        Assert.Contains(DiagnosticRuleValidator.Validate(rule),
            error => error.Contains("take", StringComparison.Ordinal));
    }

    private static DiagnosticRule CustomRule(string? takeDocument, string? take, string? check) => new()
    {
        Type = "custom",
        Code = "CSTM001",
        TakeDocument = takeDocument,
        Take = take,
        Check = check,
    };
}
