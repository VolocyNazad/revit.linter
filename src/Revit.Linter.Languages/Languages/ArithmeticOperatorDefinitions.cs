using Revit.Linter.Languages.Utils;
using StringToExpression.GrammerDefinitions;
using System.Linq.Expressions;

namespace Revit.Linter.Languages.Languages;

public static class ArithmeticOperatorDefinitions
{
    private static Dictionary<string, string> NameDictionary { get; } = new()
    {
        ["ADD"] = "add",
        ["SUBTRACT"] = "subtract",
        ["MULTIPLY"] = "multiply",
        ["DIVIDE"] = "divide",
        ["MODULO"] = "modulo",
        ["POW"] = "pow",
    };

    private static Dictionary<string, string> RegexDictionary { get; } = new()
    {
        ["ADD"] = @"\+",
        ["SUBTRACT"] = @"\-",
        ["MULTIPLY"] = @"\*",
        ["DIVIDE"] = @"\/",
        ["MODULO"] = @"\%",
        ["POW"] = @"\^",
    };

    public static IEnumerable<BinaryOperatorDefinition> Get()
        => [
            new(
                name:  NameDictionary["ADD"],
                regex: RegexDictionary["ADD"],
                orderOfPrecedence: 3,
                expressionBuilder: (left,right) =>
                    Expression.Call(
                        method: typeof(AdditionalUtils).GetMethod(
                            nameof(AdditionalUtils.Add),
                            [
                                typeof(object),
                                typeof(object)
                            ]
                        )!,
                        Expression.Convert(left, typeof(object)),
                        Expression.Convert(right, typeof(object))
                    )
                ),
            new(
                name:  NameDictionary["SUBTRACT"],
                regex: RegexDictionary["SUBTRACT"],
                orderOfPrecedence: 3,
                expressionBuilder: (left,right) => Expression.Subtract(
                    Expression.Convert(left, typeof(double)), Expression.Convert(right, typeof(double)))),
            new(
                name:  NameDictionary["MULTIPLY"],
                regex: RegexDictionary["MULTIPLY"],
                orderOfPrecedence: 2,
                expressionBuilder: (left,right) => Expression.Multiply(
                    Expression.Convert(left, typeof(double)), Expression.Convert(right, typeof(double)))),
            new(
                name:  NameDictionary["DIVIDE"],
                regex: RegexDictionary["DIVIDE"],
                orderOfPrecedence: 2,
                expressionBuilder: (left,right) => Expression.Divide(
                    Expression.Convert(left, typeof(double)), Expression.Convert(right, typeof(double)))),
            new(
                name:  NameDictionary["MODULO"],
                regex: RegexDictionary["MODULO"],
                orderOfPrecedence: 2,
                expressionBuilder: (left,right) => Expression.Modulo(
                    Expression.Convert(left, typeof(double)), Expression.Convert(right, typeof(double)))),
            new(
                name:  NameDictionary["POW"],
                regex: RegexDictionary["POW"],
                orderOfPrecedence: 1,
                expressionBuilder: (left,right) =>
                    Expression.Call(
                    method: typeof(Math).GetMethod(
                        nameof(Math.Pow),
                        [
                            typeof(double),
                            typeof(double)
                        ]
                    )!,
                    Expression.Convert(left, typeof(double)), 
                    Expression.Convert(right, typeof(double)))),
        ];
}
