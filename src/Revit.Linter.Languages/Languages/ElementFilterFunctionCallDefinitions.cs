using Revit.Linter.Languages.Utils;
using StringToExpression.GrammerDefinitions;
using System.Linq.Expressions;

namespace Revit.Linter.Languages.Languages;

internal static class ElementFilterFunctionCallDefinitions
{
    private static Dictionary<string, string> NameDictionary { get; } = new()
    {
        ["BUILTINCATEGORY"] = "builtincategory",
        ["CLASS"] = "class",
    };

    private static Dictionary<string, string> RegexDictionary { get; } = NameDictionary
        .ToDictionary(i => i.Key, i => $@"{i.Value}\(");

    // Negated leaves use the inverted native overloads, so they stay as fast as the positive ones.
    private static Dictionary<string, string> NegatedRegexDictionary { get; } = NameDictionary
        .ToDictionary(i => i.Key, i => $@"!\s*{i.Value}\(");

    public static FunctionCallDefinition[] Get()
        => [
            .. Negated(),
            new FunctionCallDefinition(
                name: NameDictionary["BUILTINCATEGORY"],
                regex: RegexDictionary["BUILTINCATEGORY"],
                argumentTypes: [typeof(string)],
                expressionBuilder: parameters =>
                    parameters[0] is ConstantExpression { Value: string builtInCategoryName }
                    ? Expression.Constant(
                        new ElementCategoryFilter((BuiltInCategory)Enum.Parse(typeof(BuiltInCategory), builtInCategoryName)), typeof(ElementFilter))
                    : Expression.Call(
                        typeof(ElementFilterUtils).GetMethod(nameof(ElementFilterUtils.GetElementCategoryFilter), [typeof(string)])!,
                        parameters)
            ),
            new FunctionCallDefinition(
                name: NameDictionary["CLASS"],
                regex: RegexDictionary["CLASS"],
                argumentTypes: [typeof(string)],
                expressionBuilder: parameters =>
                    parameters[0] is ConstantExpression { Value: string className }
                    ? Expression.Constant(ElementFilterUtils.GetElementClassFilter(className), typeof(ElementFilter))
                    : Expression.Call(
                        typeof(ElementFilterUtils).GetMethod(nameof(ElementFilterUtils.GetElementClassFilter), [typeof(string)])!,
                        parameters)
            ),
        ];

    private static FunctionCallDefinition[] Negated()
        => [
            new FunctionCallDefinition(
                name: "NOT_" + NameDictionary["BUILTINCATEGORY"],
                regex: NegatedRegexDictionary["BUILTINCATEGORY"],
                argumentTypes: [typeof(string)],
                expressionBuilder: parameters =>
                    parameters[0] is ConstantExpression { Value: string builtInCategoryName }
                    ? Expression.Constant(
                        new ElementCategoryFilter((BuiltInCategory)Enum.Parse(typeof(BuiltInCategory), builtInCategoryName), true), typeof(ElementFilter))
                    : Expression.Call(
                        typeof(ElementFilterUtils).GetMethod(nameof(ElementFilterUtils.GetInvertedElementCategoryFilter), [typeof(string)])!,
                        parameters)
            ),
            new FunctionCallDefinition(
                name: "NOT_" + NameDictionary["CLASS"],
                regex: NegatedRegexDictionary["CLASS"],
                argumentTypes: [typeof(string)],
                expressionBuilder: parameters =>
                    parameters[0] is ConstantExpression { Value: string className }
                    ? Expression.Constant(ElementFilterUtils.GetInvertedElementClassFilter(className), typeof(ElementFilter))
                    : Expression.Call(
                        typeof(ElementFilterUtils).GetMethod(nameof(ElementFilterUtils.GetInvertedElementClassFilter), [typeof(string)])!,
                        parameters)
            ),
        ];
}
