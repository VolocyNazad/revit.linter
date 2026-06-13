using Autodesk.Revit.DB.Architecture;
using StringToExpression.GrammerDefinitions;
using System.Linq.Expressions;

namespace Revit.Linter.Languages.Languages;

public static class ElementFilterOperandDefinitions
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
        .ToDictionary(i => i.Key, i => $@"{i.Value}");

    public static OperandDefinition[] Get()
        => [
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
                    Expression.Constant(new LogicalOrFilter(
                        new ElementIsElementTypeFilter(),
                        new ElementIsElementTypeFilter(true)
                    ), typeof(ElementFilter))
            ),
            new OperandDefinition(
                name: NameDictionary["EMPTY"],
                regex: RegexDictionary["EMPTY"],
                expressionBuilder: _ =>
                    Expression.Constant(new LogicalAndFilter(
                        new ElementIsElementTypeFilter(),
                        new ElementIsElementTypeFilter(true)
                    ), typeof(ElementFilter))
            ),
        ];
}

