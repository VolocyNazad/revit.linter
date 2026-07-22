using StringToExpression.GrammerDefinitions;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

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
                expressionBuilder: x => Expression.Constant(Unescape(x[1..^1]))),
        ];

    private static string Unescape(string value) => Regex.Replace(
        value,
        @"\\(['\\rfnbtn])",
        match => match.Groups[1].Value[0] switch
        {
            '\'' => "'",
            '\\' => "\\",
            'r' => "\r",
            'f' => "\f",
            'n' => "\n",
            'b' => "\b",
            't' => "\t",
            _ => match.Value,
        });
}
