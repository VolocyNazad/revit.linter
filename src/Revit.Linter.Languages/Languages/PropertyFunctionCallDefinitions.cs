using StringToExpression.GrammerDefinitions;
using System.Linq.Expressions;

namespace Revit.Linter.Languages.Languages;

public static class PropertyFunctionCallDefinitions
{
    private static Dictionary<string, string> NameDictionary { get; } = new()
    {
        ["PROPERTY"] = "property",
    };

    private static Dictionary<string, string> RegexDictionary { get; } = NameDictionary
        .ToDictionary(i => i.Key, i => $@"{i.Value}\(");

    public static FunctionCallDefinition[] Get(Expression elementExpression)
    => [
        new FunctionCallDefinition(
            name:  NameDictionary["PROPERTY"],
            regex: RegexDictionary["PROPERTY"],
            argumentTypes: [typeof(string)],
            expressionBuilder: parameters => {
                Expression propertyNameExpression = parameters[0];
                if (propertyNameExpression is ConstantExpression { Value: string propertyName })
                    return Expression.Property(elementExpression, propertyName);
                return null;
            }
        ),
    ];
}

