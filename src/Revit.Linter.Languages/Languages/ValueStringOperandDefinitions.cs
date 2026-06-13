using StringToExpression.GrammerDefinitions;
using System.Linq.Expressions;

namespace Revit.Linter.Languages.Languages;

public static class ValueStringOperandDefinitions
{
    private static Dictionary<string, string> NameDictionary { get; } = new()
    {
        ["STRING"] = "string",
    };

    private static Dictionary<string, string> RegexDictionary { get; } = new()
    {
        ["STRING"] = @"'(?:\\.|[^'])*'",
    };

    public static IEnumerable<OperandDefinition> Get()
        => [
            new(
                name: NameDictionary["STRING"],
                regex: RegexDictionary["STRING"],
                expressionBuilder: x => Expression.Constant(x.Trim('\'')
                    .Replace("\\'", "'")
                    .Replace("\\r", "\r")
                    .Replace("\\f", "\f")
                    .Replace("\\n", "\n")
                    .Replace("\\\\", "\\")
                    .Replace("\\b", "\b")
                    .Replace("\\t", "\t"))),
        ];
}
