using Revit.Linter.ParameterElementDiagnostics.Infrastructure.Utils;
using Revit.Linter.ParameterElementDiagnostics.Models;

namespace Revit.Linter.UserDiagnostics.Tests;

public sealed class ParameterConfigurationErrorFormatterTests
{
    [Fact]
    public void Invalid_take_formula_is_reported_with_rule_code_and_formula()
    {
        const string formula = "if(true,,false,true)";
        ParameterConfigurationError error = new(
            ParameterConfigurationErrorKind.InvalidFormula, "PRM001", null, formula);

        string text = ParameterConfigurationErrorFormatter.Format("diagnostics.config.yaml", [error]);

        Assert.Contains("PRM001", text, StringComparison.Ordinal);
        Assert.Contains(formula, text, StringComparison.Ordinal);
        Assert.Contains("take", text, StringComparison.OrdinalIgnoreCase);
    }
}
