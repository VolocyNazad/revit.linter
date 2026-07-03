using Revit.Linter.Languages.Utils;
using StringToExpression.GrammerDefinitions;
using System.Linq.Expressions;
using System.Reflection;

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
                {
                    var prop = elementExpression.Type.GetProperty(propertyName);
                    if (prop != null)
                        return Expression.Property(elementExpression, propertyName);

                    return Expression.Call(
                        typeof(ReflectionUtils).GetMethod(nameof(ReflectionUtils.GetPropertyValue), [typeof(object), typeof(string)]),
                        elementExpression,
                        Expression.Constant(propertyName)
                    );
                }

                return Expression.Call(
                    typeof(ReflectionUtils).GetMethod(nameof(ReflectionUtils.GetPropertyValue), [typeof(object), typeof(string)]), 
                    elementExpression, 
                    propertyNameExpression
                );
            }
        ),
    ];
}
