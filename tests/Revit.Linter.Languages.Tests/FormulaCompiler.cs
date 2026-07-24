using Revit.Linter.Languages.Languages;
using StringToExpression;
using System.Linq.Expressions;

namespace Revit.Linter.Languages.Tests;

internal static class FormulaCompiler
{
    private static readonly Language Language = new(LanguageDefinitions.CreateCommon());

    public static T Evaluate<T>(string formula)
    {
        Expression expression = Language.Parse(formula);
        Expression convertedExpression = Expression.Convert(expression, typeof(T));
        return Expression.Lambda<Func<T>>(convertedExpression).Compile().Invoke();
    }

    public static TResult Evaluate<TTarget, TResult>(string formula, TTarget target)
        => Compile<TTarget, TResult>(formula).Invoke(target);

    public static Func<TTarget, TResult> Compile<TTarget, TResult>(string formula)
    {
        ParameterExpression targetExpression = Expression.Parameter(typeof(TTarget));
        Language language = new(LanguageDefinitions.CreateForDocument(targetExpression));
        Expression expression = language.Parse(formula);
        Expression convertedExpression = Expression.Convert(expression, typeof(TResult));
        return Expression.Lambda<Func<TTarget, TResult>>(convertedExpression, targetExpression)
            .Compile();
    }

}
