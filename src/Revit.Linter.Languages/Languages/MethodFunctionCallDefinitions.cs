using Revit.Linter.Languages.Utils;
using StringToExpression.GrammerDefinitions;
using System.Linq.Expressions;
using System.Reflection;

namespace Revit.Linter.Languages.Languages;

public static class MethodFunctionCallDefinitions
{
    public static FunctionCallDefinition[] Get(Expression targetExpression)
        =>
        [
            new FunctionCallDefinition(
                name: "method",
                regex: @"method\(",
                argumentTypes: [typeof(string)],
                expressionBuilder: parameters => BuildExpression(targetExpression, parameters[0]))
        ];

    private static Expression BuildExpression(Expression targetExpression, Expression methodNameExpression)
    {
        if (methodNameExpression is ConstantExpression { Value: string methodName })
        {
            MethodInfo? method = targetExpression.Type
                .GetMethods()
                .FirstOrDefault(method =>
                    method.Name == methodName &&
                    method.GetParameters().Length == 0 &&
                    !method.IsGenericMethodDefinition &&
                    method.ReturnType != typeof(void));

            if (method is not null)
                return Expression.Call(targetExpression, method);
        }

        return Expression.Call(
            typeof(ReflectionUtils).GetMethod(
                nameof(ReflectionUtils.InvokeMethod),
                [typeof(object), typeof(string)])!,
            targetExpression,
            methodNameExpression);
    }
}
