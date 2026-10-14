using Autodesk.Revit.DB.Architecture;
using StringToExpression.GrammerDefinitions;
using System.Linq.Expressions;
using Revit.Sugar;

namespace Revit.Linter.Languages.Languages;

internal static class ElementFilterOperandDefinitions
{
    private static Dictionary<string, string> NameDictionary { get; } = new()
    {
        ["TYPE"] = "type",
        ["INSTANCE"] = "instance",
        ["ROOM"] = "room",
        ["ALL"] = "all",
        ["EMPTY"] = "empty",
    };

    private static Dictionary<string, string> RegexDictionary { get; } = NameDictionary
        .ToDictionary(i => i.Key, i => $@"{i.Value}\b");

    // Negated leaves use the inverted native overloads, so they stay as fast as the positive ones.
    // Compound negation such as !(A and B) is not supported: rewrite it through De Morgan (!A or !B).
    private static Dictionary<string, string> NegatedRegexDictionary { get; } = NameDictionary
        .ToDictionary(i => i.Key, i => $@"!\s*{i.Value}\b");

    public static OperandDefinition[] Get()
        => [
            .. Negated(),
            new OperandDefinition(
                name: NameDictionary["TYPE"],
                regex: RegexDictionary["TYPE"],
                expressionBuilder: _ =>
                    Expression.Constant(new ElementIsElementTypeFilter(), typeof(ElementFilter))
            ),
            new OperandDefinition(
                name: NameDictionary["INSTANCE"],
                regex: RegexDictionary["INSTANCE"],
                expressionBuilder: _ =>
                    Expression.Constant(new ElementIsElementTypeFilter(true), typeof(ElementFilter))
            ),
            new OperandDefinition(
                name: NameDictionary["ROOM"],
                regex: RegexDictionary["ROOM"],
                expressionBuilder: _ =>
                    Expression.Constant(new RoomFilter(), typeof(ElementFilter))),
            new OperandDefinition(
                name: NameDictionary["ALL"],
                regex: RegexDictionary["ALL"],
                expressionBuilder: _ =>
                    Expression.Constant(ElementFilterUtils.AllFilter(), typeof(ElementFilter))
            ),
            new OperandDefinition(
                name: NameDictionary["EMPTY"],
                regex: RegexDictionary["EMPTY"],
                expressionBuilder: _ =>
                    Expression.Constant(ElementFilterUtils.EmptyFilter(), typeof(ElementFilter))
            ),
        ];

    private static OperandDefinition[] Negated()
        => [
            new OperandDefinition(
                name: "NOT_" + NameDictionary["TYPE"],
                regex: NegatedRegexDictionary["TYPE"],
                expressionBuilder: _ =>
                    Expression.Constant(new ElementIsElementTypeFilter(true), typeof(ElementFilter))
            ),
            new OperandDefinition(
                name: "NOT_" + NameDictionary["INSTANCE"],
                regex: NegatedRegexDictionary["INSTANCE"],
                expressionBuilder: _ =>
                    Expression.Constant(new ElementIsElementTypeFilter(), typeof(ElementFilter))
            ),
            // RoomFilter has no inverted overload, so !room stays unsupported.
            new OperandDefinition(
                name: "NOT_" + NameDictionary["ALL"],
                regex: NegatedRegexDictionary["ALL"],
                expressionBuilder: _ =>
                    Expression.Constant(ElementFilterUtils.EmptyFilter(), typeof(ElementFilter))
            ),
            new OperandDefinition(
                name: "NOT_" + NameDictionary["EMPTY"],
                regex: NegatedRegexDictionary["EMPTY"],
                expressionBuilder: _ =>
                    Expression.Constant(ElementFilterUtils.AllFilter(), typeof(ElementFilter))
            ),
        ];
}
