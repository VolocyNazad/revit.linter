using StringToExpression.GrammerDefinitions;
using System.Globalization;
using System.Linq.Expressions;

namespace Revit.Linter.Languages.Languages;

public static class ValueArithmeticOperandDefinitions
{
    private static Dictionary<string, string> NameDictionary { get; } = new()
    {
        ["DOUBLE"] = "double",
    };

    private static Dictionary<string, string> RegexDictionary { get; } = new()
    {
        ["DOUBLE"] = @"\-?\d+(\.\d+)?",
    };

    public static IEnumerable<OperandDefinition> Get()
        => [
            new(
                name:   NameDictionary["DOUBLE"],
                regex:  RegexDictionary["DOUBLE"],
                expressionBuilder: x => Expression.Constant(double.Parse(x, CultureInfo.InvariantCulture))),
        ];
}