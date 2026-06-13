using StringToExpression.GrammerDefinitions;
using System.Linq.Expressions;

namespace Revit.Linter.Languages.Languages;

public static class ValueBooleanOperandDefinitions
{
    private static Dictionary<string, string> NameDictionary { get; } = new()
    {
        ["TRUE"] = "true",
        ["FALSE"] = "false",
    };

    private static Dictionary<string, string> RegexDictionary { get; } = NameDictionary
        .ToDictionary(i => i.Key, i => $@"{i.Value}");

    public static IEnumerable<OperandDefinition> Get()
        => [
            new(
                name: NameDictionary["TRUE"],
                regex: RegexDictionary["TRUE"],
                expressionBuilder: _ => Expression.Constant(true)),
            new(
                name: NameDictionary["FALSE"],
                regex: RegexDictionary["FALSE"],
                expressionBuilder: _ => Expression.Constant(false))
        ];
}
