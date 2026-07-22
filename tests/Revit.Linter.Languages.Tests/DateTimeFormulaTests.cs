using System.Globalization;

namespace Revit.Linter.Languages.Tests;

public sealed class DateTimeFormulaTests
{
    [Fact]
    public void Now_returns_current_time_in_requested_format()
    {
        DateTime before = DateTime.Now;

        string result = FormulaCompiler.Evaluate<string>("now('O')");

        DateTime after = DateTime.Now;
        DateTime parsed = DateTime.ParseExact(result, "O", CultureInfo.CurrentCulture);
        Assert.InRange(parsed, before, after);
    }

    [Fact]
    public void Now_result_can_participate_in_string_expression()
    {
        int yearBefore = DateTime.Now.Year;
        string result = FormulaCompiler.Evaluate<string>("'Year: ' + now('yyyy')");
        int yearAfter = DateTime.Now.Year;

        Assert.Contains(result, new[] { $"Year: {yearBefore}", $"Year: {yearAfter}" });
    }
}
