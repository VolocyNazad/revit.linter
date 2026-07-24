using Humanizer;
using StringToExpression.GrammerDefinitions;
using System.Globalization;
using System.Linq.Expressions;

namespace Revit.Linter.Languages.Languages;

public static class StringFunctionCallDefinitions
{
    private static Dictionary<string, string> NameDictionary { get; } = new()
    {
        ["STR"] = "str",
        ["CONTAINS"] = "contains",
        ["STARTWITH"] = "startwith",
        ["ENDWITH"] = "endwith",
        ["TOLOWER"] = "tolower",
        ["TOUPPER"] = "toupper",
        ["TRIM"] = "trim",
        ["REPLACE"] = "replace",
        ["LENGTH"] = "length",
        ["SUBSTRING"] = "substring",
        ["TOTITLE"] = "totitle",
        ["TOSENTENCE"] = "tosentence",
    };

    private static Dictionary<string, string> RegexDictionary { get; } = NameDictionary
        .ToDictionary(i => i.Key, i => $@"{i.Value}\(");

    public static IEnumerable<FunctionCallDefinition> Get()
        => [
            new(
                name:  NameDictionary["STR"],
                regex: RegexDictionary["STR"],
                argumentTypes: [typeof(object)],
                expressionBuilder: args =>
                {
                    Type[] signature = [typeof(object), typeof(CultureInfo)];
                    return Expression.Call(
                        method:typeof(Convert).GetMethod(nameof(Convert.ToString), signature)!,
                        Expression.Convert(args[0], typeof(object)),
                        Expression.Constant(CultureInfo.InvariantCulture)
                    );
                }),

            new(
                name:  NameDictionary["CONTAINS"],
                regex: RegexDictionary["CONTAINS"],
                argumentTypes: [typeof(string), typeof(string)],
                expressionBuilder: args =>
                {
                    return Expression.Call(
                        args[0],
                        method:typeof(string).GetMethod(
                            nameof(string.Contains), [typeof(string)])!,
                        Expression.Convert(args[1], typeof(string))
                    );
                }),

            new(
                name:  NameDictionary["STARTWITH"],
                regex: RegexDictionary["STARTWITH"],
                argumentTypes: [typeof(string), typeof(string)],
                expressionBuilder: args =>
                {
                    return Expression.Call(
                        args[0],
                        method:typeof(string).GetMethod(
                            nameof(string.StartsWith),
                            [typeof(string)])!,
                        Expression.Convert(args[1], typeof(string))
                    );
                }),

            new(
                name:  NameDictionary["ENDWITH"],
                regex: RegexDictionary["ENDWITH"],
                argumentTypes: [typeof(string), typeof(string)],
                expressionBuilder: args =>
                {
                    return Expression.Call(
                        args[0],
                        method:typeof(string).GetMethod(
                            nameof(string.EndsWith),
                            [typeof(string)])!,
                        Expression.Convert(args[1], typeof(string))
                    );
                }),

            new(
                name:  NameDictionary["TOLOWER"],
                regex: RegexDictionary["TOLOWER"],
                argumentTypes: [typeof(string)],
                expressionBuilder: args =>
                {
                    return Expression.Call(
                        args[0],
                        method:typeof(string).GetMethod(nameof(string.ToLower), [])!
                    );
                }),

            new(
                name:  NameDictionary["TOUPPER"],
                regex: RegexDictionary["TOUPPER"],
                argumentTypes: [typeof(string)],
                expressionBuilder: args =>
                {
                    return Expression.Call(
                        args[0],
                        method:typeof(string).GetMethod(nameof(string.ToUpper), [])!
                    );
                }),

            new(
                name:  NameDictionary["TRIM"],
                regex: RegexDictionary["TRIM"],
                argumentTypes: [typeof(string)],
                expressionBuilder: args => Expression.Call(
                    args[0],
                    method: typeof(string).GetMethod(nameof(string.Trim), Type.EmptyTypes)!)),

            new(
                name:  NameDictionary["REPLACE"],
                regex: RegexDictionary["REPLACE"],
                argumentTypes: [typeof(string), typeof(string), typeof(string)],
                expressionBuilder: args => Expression.Call(
                    args[0],
                    method: typeof(string).GetMethod(
                        nameof(string.Replace),
                        [typeof(string), typeof(string)])!,
                    args[1],
                    args[2])),

            new(
                name:  NameDictionary["LENGTH"],
                regex: RegexDictionary["LENGTH"],
                argumentTypes: [typeof(string)],
                expressionBuilder: args => Expression.Convert(
                    Expression.Property(args[0], nameof(string.Length)),
                    typeof(double))),

            new(
                name:  NameDictionary["SUBSTRING"],
                regex: RegexDictionary["SUBSTRING"],
                argumentTypes: [typeof(string), typeof(double), typeof(double)],
                expressionBuilder: args => Expression.Call(
                    args[0],
                    method: typeof(string).GetMethod(
                        nameof(string.Substring),
                        [typeof(int), typeof(int)])!,
                    Expression.Convert(args[1], typeof(int)),
                    Expression.Convert(args[2], typeof(int)))),

            new(
                name:  NameDictionary["TOTITLE"],
                regex: RegexDictionary["TOTITLE"],
                argumentTypes: [typeof(string)],
                expressionBuilder: args =>
                {
                    Expression culturedStringTransformer = Expression.Call(
                        method:(typeof(To).GetProperty(nameof(To.TitleCase)) ?? throw new MissingMethodException()).GetGetMethod() ?? throw new MissingMethodException());

                    return Expression.Call(
                        method:typeof(To).GetMethod(nameof(To.Transform), [typeof(string), typeof(ICulturedStringTransformer[])])!,
                        args[0],
                        Expression.NewArrayInit(typeof(ICulturedStringTransformer), culturedStringTransformer)
                    );
                }),

            new(
                name:  NameDictionary["TOSENTENCE"],
                regex: RegexDictionary["TOSENTENCE"],
                argumentTypes: [typeof(string)],
                expressionBuilder: args =>
                {
                    Expression culturedStringTransformer = Expression.Call(
                        method:(typeof(To).GetProperty(nameof(To.SentenceCase)) ?? throw new MissingMethodException()).GetGetMethod() ?? throw new MissingMethodException());

                    return Expression.Call(
                        method:typeof(To).GetMethod(nameof(To.Transform), [typeof(string), typeof(ICulturedStringTransformer[])])!,
                        args[0],
                        Expression.NewArrayInit(typeof(ICulturedStringTransformer), culturedStringTransformer)
                    );
                }),
        ];
}
