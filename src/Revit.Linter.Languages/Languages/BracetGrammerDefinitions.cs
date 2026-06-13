using StringToExpression.GrammerDefinitions;

namespace Revit.Linter.Languages.Languages;

public static class BracetGrammerDefinitions
{
    private static Dictionary<string, string> NameDictionary { get; } = new()
    {
        ["OPEN_BRACKET"] = "OPEN_BRACKET",
        ["CLOSE_BRACKET"] = "CLOSE_BRACKET",
        ["COMMA"] = "COMMA",
    };

    private static Dictionary<string, string> RegexDictionary { get; } = new()
    {
        ["OPEN_BRACKET"] = @"\(",
        ["COMMA"] = ",",
        ["CLOSE_BRACKET"] = @"\)",
    };

    public static IEnumerable<GrammerDefinition> Get(IEnumerable<FunctionCallDefinition> functionCalls)
    {
        BracketOpenDefinition openBracket;
        ListDelimiterDefinition delimeter;
        return [
            openBracket = new BracketOpenDefinition(
                name: NameDictionary["OPEN_BRACKET"],
                regex: RegexDictionary["OPEN_BRACKET"]),
            delimeter = new ListDelimiterDefinition(
                name: NameDictionary["COMMA"],
                regex: RegexDictionary["COMMA"]),
            new BracketCloseDefinition(
                name: NameDictionary["CLOSE_BRACKET"],
                regex: RegexDictionary["CLOSE_BRACKET"],
                bracketOpenDefinitions: new[] { openBracket }.Concat(functionCalls),
                listDelimeterDefinition: delimeter)
        ];
    }
}
