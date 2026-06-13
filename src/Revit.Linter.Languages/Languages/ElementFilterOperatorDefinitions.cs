using StringToExpression.GrammerDefinitions;
using System.Linq.Expressions;

namespace Revit.Linter.Languages.Languages;

public static class ElementFilterOperatorDefinitions
{
    public static BinaryOperatorDefinition[] Get()
        => [
            new BinaryOperatorDefinition(
                name:  "and",
                regex: "and",
                orderOfPrecedence: 25,
                expressionBuilder: (left, right) => {
                    Type[] signature = [typeof(ElementFilter),  typeof(ElementFilter)];
                    var ctor = typeof(LogicalAndFilter).GetConstructor(signature)!;
                    return Expression.New(ctor, left, right);
                }),
            new BinaryOperatorDefinition(
                name:  "or",
                regex: "or",
                orderOfPrecedence: 26,
                expressionBuilder: (left, right) => {
                    Type[] signature = [typeof(ElementFilter),  typeof(ElementFilter)];
                    var ctor = typeof(LogicalOrFilter).GetConstructor(signature)!;
                    return Expression.New(ctor, left, right);
                }),
        ];
}

