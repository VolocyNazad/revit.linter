using StringToExpression.GrammerDefinitions;
using System.Linq.Expressions;

namespace Revit.Linter.Languages.Languages;

public static class LogicalFunctionCallDefinitions
{
    // todo if without else

    private static Dictionary<string, string> NameDictionary { get; } = new()
    {
        ["IF"] = "if",
        ["ISNULL"] = "isnull",
        ["ISDOUBLE"] = "isdouble",
        ["ISSTRING"] = "isstring",
        ["ISBOOL"] = "isbool",
        ["ISNULLOREMPTY"] = "isnullorempty",
    };

    private static Dictionary<string, string> RegexDictionary { get; } = NameDictionary
        .ToDictionary(i => i.Key, i => $@"{i.Value}\(");

    public static IEnumerable<FunctionCallDefinition> Get()
        => [
            new(
                name:  NameDictionary["IF"],
                regex: RegexDictionary["IF"],
                argumentTypes: [typeof(bool), typeof(object), typeof(object)],
                expressionBuilder: args =>
                {
                    UnaryExpression test = Expression.Convert(args[0], typeof(bool));

                    return Expression.Condition(test, args[1], args[2]);
                }),
            new(
                name:  NameDictionary["ISNULL"],
                regex: RegexDictionary["ISNULL"],
                argumentTypes: [typeof(object)],
                expressionBuilder: args => Expression.Equal(args[0], Expression.Constant(null, typeof(object)))),
            new(
                name:  NameDictionary["ISDOUBLE"],
                regex: RegexDictionary["ISDOUBLE"],
                argumentTypes: [typeof(object)],
                expressionBuilder: args => Expression.TypeIs(args[0], typeof(double))),
            new(
                name:  NameDictionary["ISSTRING"],
                regex: RegexDictionary["ISSTRING"],
                argumentTypes: [typeof(object)],
                expressionBuilder: args => Expression.TypeIs(args[0], typeof(string))),
            new(
                name:  NameDictionary["ISBOOL"],
                regex: RegexDictionary["ISBOOL"],
                argumentTypes: [typeof(object)],
                expressionBuilder: args => Expression.TypeIs(args[0], typeof(bool))),
            new(
                name:  NameDictionary["ISNULLOREMPTY"],
                regex: RegexDictionary["ISNULLOREMPTY"],
                argumentTypes: [typeof(object)],
                expressionBuilder: args => Expression.Call(typeof(string).GetMethod(nameof(string.IsNullOrEmpty))!, Expression.Convert(args[0], typeof(string)))),
        ];
}
