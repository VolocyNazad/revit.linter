using Revit.Linter.Languages.Utils;
using StringToExpression.GrammerDefinitions;
using System.Linq.Expressions;

namespace Revit.Linter.Languages.Languages;

public static class ElementFilterFunctionCallDefinitions
{
    private static Dictionary<string, string> NameDictionary { get; } = new()
    {
        ["BUILTINCATEGORY"] = "builtincategory",
        ["CLASS"] = "class",
    };

    private static Dictionary<string, string> RegexDictionary { get; } = NameDictionary
        .ToDictionary(i => i.Key, i => $@"{i.Value}\(");

    public static FunctionCallDefinition[] Get()
        => [
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
                    ? Expression.Constant(new ElementClassFilter(RevitClassUtils.GetType(className)), typeof(ElementFilter))
                    : Expression.Call(
                        typeof(ElementFilterUtils).GetMethod(nameof(ElementFilterUtils.GetElementClassFilter), [typeof(string)])!,
                        parameters)
            ),
        ];
}

internal static class ElementFilterUtils
{
    public static ElementFilter GetElementClassFilter(string className)
    {
        Type type = RevitClassUtils.GetType(className);
        ElementClassFilter filter = new(type);
        return filter;
    }
    public static ElementFilter GetElementCategoryFilter(string builtInCategoryName)
    {
#if AFTER2025
        BuiltInCategory builtInCategory = Enum.Parse<BuiltInCategory>(builtInCategoryName);
#else
        BuiltInCategory builtInCategory = (BuiltInCategory)Enum.Parse(typeof(BuiltInCategory), builtInCategoryName);
#endif
        ElementCategoryFilter filter = new(builtInCategory);
        return filter;
    }
    public static ElementFilter GetElementWorksetFilter(Document document, string worksetName)
    {
        Workset workset = new FilteredWorksetCollector(document).ToWorksets().First(i => i.Name == worksetName);
        ElementWorksetFilter filter = new(workset.Id);
        return filter;
    }
}

