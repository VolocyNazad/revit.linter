using StringToExpression.GrammerDefinitions;
using StringToExpression.Util;
using System.Linq.Expressions;

namespace Revit.Linter.Languages.Languages;

public static class ArithmeticFunctionCallDefinitions
{
    private static Dictionary<string, string> NameDictionary { get; } = new()
    {
        ["ROUNDUP"] = "roundup",
        ["ROUNDDOWN"] = "rounddown",
        ["ROUND"] = "round",
        ["SIN"] = "sin",
        ["COS"] = "cos",
        ["TAN"] = "tan",
        ["SQRT"] = "sqrt",
        ["NUM"] = "num",
    };

    private static Dictionary<string, string> RegexDictionary { get; } = NameDictionary
        .ToDictionary(i => i.Key, i => $@"{i.Value}\(");

    public static IEnumerable<FunctionCallDefinition> Get()
        => [
            new(
                name:  NameDictionary["ROUNDUP"],
                regex: RegexDictionary["ROUNDUP"],
                argumentTypes: [typeof(double)],
                expressionBuilder: args => Expression.Call(
                    method:Type<object>.Method(x=>Math.Ceiling(0.0)),
                    arguments: args[0])),
            new(
                name:  NameDictionary["ROUNDDOWN"],
                regex: RegexDictionary["ROUNDDOWN"],
                argumentTypes: [typeof(double)],
                expressionBuilder: args => Expression.Call(
                    method:Type<object>.Method(x=>Math.Floor(0.0)),
                    arguments: args[0])),
            new(
                name:  NameDictionary["ROUND"],
                regex: RegexDictionary["ROUND"],
                argumentTypes: [typeof(double), typeof(double)],
                expressionBuilder: args => Expression.Call(
                    method:Type<object>.Method(x=>Math.Round(0.0, 0, MidpointRounding.AwayFromZero)),
                    args[0],
                    Expression.Convert(args[1], typeof(int)),
                    Expression.Constant(MidpointRounding.AwayFromZero))),
            new(
                name:  NameDictionary["SIN"],
                regex: RegexDictionary["SIN"],
                argumentTypes: [typeof(double)],
                expressionBuilder: args => Expression.Call(
                    method:Type<object>.Method(x=>Math.Sin(0)),
                    arguments: args[0])),
            new(
                name:  NameDictionary["COS"],
                regex: RegexDictionary["COS"],
                argumentTypes: [typeof(double)],
                expressionBuilder: args => Expression.Call(
                    method:Type<object>.Method(x=>Math.Cos(0)),
                    arguments: args[0])),
            new(
                name:  NameDictionary["TAN"],
                regex: RegexDictionary["TAN"],
                argumentTypes: [typeof(double)],
                expressionBuilder: args => Expression.Call(
                    method:Type<object>.Method(x=>Math.Tan(0)),
                    arguments: args[0])),
            new(
                name:  NameDictionary["SQRT"],
                regex: RegexDictionary["SQRT"],
                argumentTypes: [typeof(double)],
                expressionBuilder: args => Expression.Call(
                    method:Type<object>.Method(x=>Math.Sqrt(0)),
                    arguments: args[0])),
             new(
                name:  NameDictionary["NUM"],
                regex: RegexDictionary["NUM"],
                argumentTypes: [typeof(string)],
                expressionBuilder: args => Expression.Call(
                    method:Type<object>.Method(x=>double.Parse("")),
                    arguments: args[0])),
        ];
}