using Revit.Linter.ParameterElementDiagnostics.Infrastructure.Utils;
using Revit.Linter.ParameterElementDiagnostics.Models;

namespace Revit.Linter.UserDiagnostics.Tests;

public sealed class ParameterRuleValidatorTests
{
    private const string KnownCategory = "OST_DuctAccessory";
    private const string KnownGroup = "PG_DATA";

    [Fact]
    public void Valid_rule_has_no_errors()
    {
        IReadOnlyList<ParameterConfigurationError> errors = Validate(CreateRule(CreateParameter()));

        Assert.Empty(errors);
    }

    [Fact]
    public void Parameter_without_guid_is_valid()
    {
        IReadOnlyList<ParameterConfigurationError> errors = Validate(CreateRule(CreateParameter(guid: null)));

        Assert.Empty(errors);
    }

    [Fact]
    public void Unknown_category_is_reported_with_its_parameter_and_value()
    {
        IReadOnlyList<ParameterConfigurationError> errors = Validate(
            CreateRule(CreateParameter(categories: [KnownCategory, "OST_DuctAcessory"])));

        ParameterConfigurationError error = Assert.Single(errors);
        Assert.Equal(ParameterConfigurationErrorKind.UnknownCategory, error.Kind);
        Assert.Equal("PRMTR001", error.RuleCode);
        Assert.Equal("ADSK_Position", error.ParameterName);
        Assert.Equal("OST_DuctAcessory", error.Value);
    }

    [Fact]
    public void Unknown_group_is_reported()
    {
        IReadOnlyList<ParameterConfigurationError> errors = Validate(
            CreateRule(CreateParameter(group: "PG_UNKNOWN")));

        ParameterConfigurationError error = Assert.Single(errors);
        Assert.Equal(ParameterConfigurationErrorKind.UnknownGroup, error.Kind);
        Assert.Equal("PG_UNKNOWN", error.Value);
    }

    [Fact]
    public void Missing_group_is_checked_as_an_empty_value()
    {
        IReadOnlyList<ParameterConfigurationError> errors = Validate(CreateRule(CreateParameter(group: null)));

        ParameterConfigurationError error = Assert.Single(errors);
        Assert.Equal(ParameterConfigurationErrorKind.UnknownGroup, error.Kind);
        Assert.Equal(string.Empty, error.Value);
    }

    [Fact]
    public void Invalid_guid_is_reported()
    {
        IReadOnlyList<ParameterConfigurationError> errors = Validate(
            CreateRule(CreateParameter(guid: "not-a-guid")));

        ParameterConfigurationError error = Assert.Single(errors);
        Assert.Equal(ParameterConfigurationErrorKind.InvalidGuid, error.Kind);
        Assert.Equal("not-a-guid", error.Value);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Missing_or_empty_categories_are_reported(bool useNull)
    {
        IReadOnlyList<ParameterConfigurationError> errors = Validate(
            CreateRule(useNull ? CreateParameter(omitCategories: true) : CreateParameter(categories: [])));

        ParameterConfigurationError error = Assert.Single(errors);
        Assert.Equal(ParameterConfigurationErrorKind.MissingValue, error.Kind);
        Assert.Equal("categories", error.Value);
        Assert.Equal("ADSK_Position", error.ParameterName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Missing_or_empty_parameters_are_reported(bool useNull)
    {
        DiagnosticRule rule = useNull ? CreateRule(null) : CreateRule();

        ParameterConfigurationError error = Assert.Single(Validate(rule));
        Assert.Equal(ParameterConfigurationErrorKind.MissingValue, error.Kind);
        Assert.Equal("parameters", error.Value);
        Assert.Null(error.ParameterName);
    }

    [Fact]
    public void Missing_rule_fields_are_reported()
    {
        DiagnosticRule rule = new()
        {
            Code = " ",
            Description = string.Empty,
            Message = string.Empty,
            Take = null!,
            Parameters = [CreateParameter()]
        };

        IReadOnlyList<ParameterConfigurationError> errors = Validate(rule);

        Assert.Equal(new[] { "code", "take" }, errors.Select(error => error.Value));
        Assert.All(errors, error => Assert.Equal(ParameterConfigurationErrorKind.MissingValue, error.Kind));
    }

    [Fact]
    public void All_errors_of_a_rule_are_collected()
    {
        IReadOnlyList<ParameterConfigurationError> errors = Validate(CreateRule(
            CreateParameter(name: null, guid: "x", group: "PG_UNKNOWN", categories: ["OST_Unknown"]),
            CreateParameter()));

        Assert.Equal(
            new[]
            {
                ParameterConfigurationErrorKind.MissingValue,
                ParameterConfigurationErrorKind.InvalidGuid,
                ParameterConfigurationErrorKind.UnknownGroup,
                ParameterConfigurationErrorKind.UnknownCategory
            },
            errors.Select(error => error.Kind));
    }

    private static IReadOnlyList<ParameterConfigurationError> Validate(DiagnosticRule rule) =>
        ParameterRuleValidator.Validate(
            rule,
            category => category == KnownCategory,
            group => group == KnownGroup);

    private static DiagnosticRule CreateRule(params ParameterElementData[]? parameters) => new()
    {
        Code = "PRMTR001",
        Description = "custom",
        Message = "message",
        Take = "!property('IsFamilyDocument')",
        Parameters = parameters?.ToList()!
    };

    private static ParameterElementData CreateParameter(
        string? name = "ADSK_Position",
        string? guid = "ae8ff999-1f22-4ed7-ad33-61503d85f0f4",
        string? group = KnownGroup,
        string[]? categories = null,
        bool omitCategories = false) => new()
    {
        Name = name!,
        Guid = guid!,
        Group = group!,
        IsInstance = true,
        AllowVaryBetweenGroups = true,
        Categories = omitCategories ? null! : categories?.ToList() ?? [KnownCategory]
    };
}
