using StringToExpression.GrammerDefinitions;
using System.Linq.Expressions;

namespace Revit.Linter.Languages.Languages;

public static class ElementFunctionCallDefinitions
{
    private static Dictionary<string, string> NameDictionary { get; } = new()
    {
        ["PARAMETER"] = "parameter",
    };

    private static Dictionary<string, string> RegexDictionary { get; } = NameDictionary
        .ToDictionary(i => i.Key, i => $@"{i.Value}\(");

    public static FunctionCallDefinition[] Get(Expression elementExpression)
    => [
        new FunctionCallDefinition(
            name:  NameDictionary["PARAMETER"],
            regex: RegexDictionary["PARAMETER"],
            argumentTypes: [typeof(string)],
            expressionBuilder: parameters => {
                Expression identifierExpression = parameters[0];

                Expression? parameterExpression = identifierExpression is ConstantExpression { Value: string elementIdentifier }
                    ? CreateGetParameterExpression(elementExpression, elementIdentifier)
                    : CreateGetParameterExpression(elementExpression, identifierExpression);

                Expression valueExpression = Expression.Call(
                    typeof(Utils).GetMethod(nameof(Utils.GetParameterValue), [typeof(Parameter)])!,
                    parameterExpression
                );

                return valueExpression;
            }

        ),  // todo Добавить перегрузку, чтобы пользователь сам указывал тип параметра
    ];

    private static Expression CreateGetParameterExpression(Expression elementExpression, Expression elementIdentifierExpression)
    {
        Expression? parameterExpression = Expression.Call(
            typeof(Utils).GetMethod(nameof(Utils.DynamicGetParameter), [typeof(Element), typeof(string)])!,
            elementExpression,
            elementIdentifierExpression
        );

        return parameterExpression;
    }

    private static Expression CreateGetParameterExpression(Expression elementExpression, string elementIdentifier)
    {
        Expression parameterExpression;

        Type type = Utils.GetIdentifierType(elementIdentifier);

        if (type == typeof(BuiltInParameter)) {
#if AFTER2025
            BuiltInParameter bip = Enum.Parse<BuiltInParameter>(elementIdentifier);
#else
            BuiltInParameter bip = (BuiltInParameter)Enum.Parse(typeof(BuiltInParameter), elementIdentifier);
#endif
            parameterExpression = Expression.Call(
                typeof(Utils).GetMethod(nameof(Utils.GetParameter), [typeof(Element), typeof(BuiltInParameter)])!,
                elementExpression,
                Expression.Constant(bip)
            );
        }
        else if (type == typeof(Guid)) {
            var guid = Guid.Parse(elementIdentifier);
            parameterExpression = Expression.Call(
                typeof(Utils).GetMethod(nameof(Utils.GetParameter), [typeof(Element), typeof(Guid)])!,
                elementExpression,
                Expression.Constant(guid)
            );
        }

        else {
            parameterExpression = Expression.Call(
                typeof(Utils).GetMethod(nameof(Utils.GetParameter), [typeof(Element), typeof(string)])!,
                elementExpression,
                Expression.Constant(elementIdentifier)
            );
        }

        return parameterExpression;
    }
}

internal static class Utils
{
    public static Type GetIdentifierType(string identifier)
    {
        if (Enum.TryParse(identifier, out BuiltInParameter _))
            return typeof(BuiltInParameter);

        if (Guid.TryParse(identifier, out Guid _))
            return typeof(Guid);

        return typeof(string);
    }

    public static Parameter GetParameter(Element element, BuiltInParameter identifier)
        => element.get_Parameter(identifier);

    public static Parameter GetParameter(Element element, Guid identifier)
        => element.get_Parameter(identifier);

    public static Parameter GetParameter(Element element, string identifier)
        => element.LookupParameter(identifier);

    public static Parameter DynamicGetParameter(Element element, string identifier)
    {
        if (Enum.TryParse(identifier, out BuiltInParameter builtInParameter))
            return element.get_Parameter(builtInParameter);

        if (Guid.TryParse(identifier, out Guid guid))
            return element.get_Parameter(guid);

        return element.LookupParameter(identifier);
    }
    public static object? GetParameterValue(Parameter? parameter)
    {
        if (parameter == null) return null;

        object? value = parameter.StorageType switch
        {
            StorageType.String => parameter.AsString(),
            StorageType.Integer => Convert.ToDouble(parameter.AsInteger()),
            StorageType.Double => UnitUtils.ConvertFromInternalUnits(parameter.AsDouble(), GetUnitTypeId(parameter)),
            StorageType.ElementId => parameter.AsElementId(),
            StorageType.None => throw new InvalidOperationException(),
            _ => throw new NotImplementedException(),
        };
        return value;
    }
    private static ForgeTypeId GetUnitTypeId(Parameter parameter)
    {
        FormatOptions formatOptions = parameter.Element.Document.GetUnits().GetFormatOptions(SpecTypeId.Length);
        ForgeTypeId unitTypeId = formatOptions.GetUnitTypeId();
        return unitTypeId;
    }
}

