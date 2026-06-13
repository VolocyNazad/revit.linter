using StringToExpression.GrammerDefinitions;
using System.Linq.Expressions;

namespace Revit.Linter.Languages.Languages;

public static class OperandDefinitions
{
    private static Dictionary<string, string> NameDictionary { get; } = new()
    {
        ["NULL"] = "null",
    };

    private static Dictionary<string, string> RegexDictionary { get; } = NameDictionary
        .ToDictionary(i => i.Key, i => $@"{i.Value}\(");

    public static IEnumerable<OperandDefinition> Get()
        => [
            new(
                name:  NameDictionary["NULL"],
                regex: RegexDictionary["NULL"],
                expressionBuilder: _ => Expression.Constant(null)),
        ];
}
