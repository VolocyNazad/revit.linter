using StringToExpression.GrammerDefinitions;
using StringToExpression.Util;
using System.Linq.Expressions;
using System.Reflection;

namespace Revit.Linter.Languages.Languages;

public static class DateTimeFunctionCallDefinitions
{
    private static Dictionary<string, string> NameDictionary { get; } = new()
    {
        ["NOW"] = "now",
    };

    private static Dictionary<string, string> RegexDictionary { get; } = NameDictionary
        .ToDictionary(i => i.Key, i => $@"{i.Value}\(");

    public static IEnumerable<FunctionCallDefinition> Get()
        => [
            new(
                name:  NameDictionary["NOW"],
                regex: RegexDictionary["NOW"],
                argumentTypes: [typeof(string)],
                expressionBuilder: args =>
                {
                    PropertyInfo? propertyInfo = typeof(DateTime).GetProperty(
                        nameof(DateTime.Now),
                        BindingFlags.Public | BindingFlags.Static
                    );

                    MemberExpression nowDateTimeExpression = Expression.Property(null, propertyInfo!);

                    return Expression.Call(
                        nowDateTimeExpression,
                        method:Type<DateTime>.Method(x=>x.ToString("")),
                        arguments: [args[0]]);
                }),
        ];
}
