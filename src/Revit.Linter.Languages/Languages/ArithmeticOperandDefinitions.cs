using StringToExpression.GrammerDefinitions;
using System.Linq.Expressions;

namespace Revit.Linter.Languages.Languages;

public static class ArithmeticOperandDefinitions
{
    private static Dictionary<string, string> NameDictionary { get; } = new()
    {
        ["PI"] = "pi",
    };

    private static Dictionary<string, string> RegexDictionary { get; } = NameDictionary
        .ToDictionary(i => i.Key, i => $@"{i.Value}\(");

    public static IEnumerable<OperandDefinition> Get()
        => [
            new(
                name:  NameDictionary["PI"],
                regex: RegexDictionary["PI"],
                expressionBuilder: _ => Expression.Constant(Math.PI)),
        ];
}
