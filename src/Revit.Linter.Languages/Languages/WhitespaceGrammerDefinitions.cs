using StringToExpression.GrammerDefinitions;

namespace Revit.Linter.Languages.Languages;

public static class WhitespaceGrammerDefinitions
{
    private static Dictionary<string, string> NameDictionary { get; } = new()
    {
        ["WHITESPACE"] = "whitespase",
    };

    private static Dictionary<string, string> RegexDictionary { get; } = new()
    {
        ["WHITESPACE"] = @"\s+",
    };

    public static IEnumerable<GrammerDefinition> Get()
        => [
            new(
                name: NameDictionary["WHITESPACE"],
                regex: RegexDictionary["WHITESPACE"],
                ignore: true)
        ];

}
