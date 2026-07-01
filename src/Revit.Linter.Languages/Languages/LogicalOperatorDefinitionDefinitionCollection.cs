using Revit.Linter.Languages.Utils;
using StringToExpression.GrammerDefinitions;
using System.Linq.Expressions;

namespace Revit.Linter.Languages.Languages;

public static class LogicalOperatorDefinitionDefinitions
{
    private static Dictionary<string, string> NameDictionary { get; } = new()
    {
        ["EQUALS"] = "equals",
        ["NOTEQUALS"] = "notequals",
        ["GREATERTHANOREQUAL"] = "greaterthanorequal",
        ["GREATERTHAN"] = "greaterthan",
        ["LESSTHANOREQUAL"] = "lessthanorequal",
        ["LESSTHAN"] = "lessthan",
        ["AND"] = "and",
        ["OR"] = "or",
        ["NOT"] = "not",
    };

    private static Dictionary<string, string> RegexDictionary { get; } = new()
    {
        ["EQUALS"] = @"\==",
        ["NOTEQUALS"] = @"\!=",
        ["GREATERTHANOREQUAL"] = @"\>=",
        ["GREATERTHAN"] = @"\>",
        ["LESSTHANOREQUAL"] = @"\<=",
        ["LESSTHAN"] = @"\<",
        ["AND"] = @"\&",
        ["OR"] = @"\|",
        ["NOT"] = @"\!",
    };

    public static IEnumerable<OperatorDefinition> Get()
        => [
            new BinaryOperatorDefinition(
                name:  NameDictionary["EQUALS"],
                regex: RegexDictionary["EQUALS"],
                orderOfPrecedence:11,
                expressionBuilder: (left,right) =>
                    Expression.Call(
                        method:typeof(EqualsUtils).GetMethod(
                            nameof(EqualsUtils.InternalEquals),
                            [typeof(object), typeof(object)])!,
                        Expression.Convert(left, typeof(object)),
                        Expression.Convert(right, typeof(object))
                    )),
            new BinaryOperatorDefinition(
                name:  NameDictionary["NOTEQUALS"],
                regex: RegexDictionary["NOTEQUALS"],
                orderOfPrecedence:12,
                expressionBuilder: (left,right) =>
                    Expression.Call(
                        method:typeof(EqualsUtils).GetMethod(
                            nameof(EqualsUtils.InternalNotEquals),
                            [typeof(object), typeof(object)])!,
                        Expression.Convert(left, typeof(object)),
                        Expression.Convert(right, typeof(object))
                    )),

            new BinaryOperatorDefinition(
                name:  NameDictionary["GREATERTHANOREQUAL"],
                regex: RegexDictionary["GREATERTHANOREQUAL"],
                orderOfPrecedence:14,
                expressionBuilder: (left,right) =>
                    Expression.GreaterThanOrEqual(
                        Expression.Convert(left,typeof(double)),
                        Expression.Convert(right,typeof(double)))),
            new BinaryOperatorDefinition(
                name:  NameDictionary["GREATERTHAN"],
                regex: RegexDictionary["GREATERTHAN"],
                orderOfPrecedence:13,
                expressionBuilder: (left,right) =>
                    Expression.GreaterThan(
                        Expression.Convert(left,typeof(double)),
                        Expression.Convert(right,typeof(double)))),

            new BinaryOperatorDefinition(
                name:  NameDictionary["LESSTHANOREQUAL"],
                regex: RegexDictionary["LESSTHANOREQUAL"],
                orderOfPrecedence:16,
                expressionBuilder: (left,right) =>
                    Expression.LessThanOrEqual(
                        Expression.Convert(left,typeof(double)),
                        Expression.Convert(right,typeof(double)))),
            new BinaryOperatorDefinition(
                name:  NameDictionary["LESSTHAN"],
                regex: RegexDictionary["LESSTHAN"],
                orderOfPrecedence:15,
                expressionBuilder: (left,right) =>
                    Expression.LessThan(
                        Expression.Convert(left,typeof(double)),
                        Expression.Convert(right,typeof(double)))),

            new BinaryOperatorDefinition(
                name:  NameDictionary["AND"],
                regex: RegexDictionary["AND"],
                orderOfPrecedence:17,
                expressionBuilder: (left,right) =>
                    Expression.And(
                        Expression.Convert(left,typeof(bool)),
                        Expression.Convert(right,typeof(bool)))),
            new BinaryOperatorDefinition(
                name:  NameDictionary["OR"],
                regex: RegexDictionary["OR"],
                orderOfPrecedence:18,
                expressionBuilder: (left,right) =>
                    Expression.Or(
                        Expression.Convert(left,typeof(bool)),
                        Expression.Convert(right,typeof(bool)))),

            new UnaryOperatorDefinition(
                name:  NameDictionary["NOT"],
                regex: RegexDictionary["NOT"],
                orderOfPrecedence:19,
                operandPosition: RelativePosition.Right,
                expressionBuilder: arg => Expression.Not(Expression.Convert(arg, typeof(bool))))
        ];
}
