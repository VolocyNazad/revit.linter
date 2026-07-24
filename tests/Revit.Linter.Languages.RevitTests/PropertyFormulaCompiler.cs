using Autodesk.Revit.DB;
using Revit.Linter.Languages.Languages;
using StringToExpression;
using System.Linq.Expressions;

namespace Revit.Linter.Languages.RevitTests;

internal static class PropertyFormulaCompiler
{
    public static Func<TTarget, TResult> Compile<TTarget, TResult>(string formula)
    {
        ParameterExpression targetExpression = Expression.Parameter(typeof(TTarget));
        Expression expression = new Language(LanguageDefinitions.CreateForDocument(targetExpression)).Parse(formula);
        return Expression.Lambda<Func<TTarget, TResult>>(
                Expression.Convert(expression, typeof(TResult)),
                targetExpression)
            .Compile();
    }

    public static Func<Element, TResult> CompileElement<TResult>(string formula)
    {
        ParameterExpression elementExpression = Expression.Parameter(typeof(Element));
        Expression expression = new Language(LanguageDefinitions.CreateForElement(elementExpression)).Parse(formula);
        return Expression.Lambda<Func<Element, TResult>>(
                Expression.Convert(expression, typeof(TResult)),
                elementExpression)
            .Compile();
    }

    public static ElementFilter CompileElementFilter(string formula)
    {
        Expression expression = new Language(LanguageDefinitions.CreateElementFilter()).Parse(formula);
        return Expression.Lambda<Func<ElementFilter>>(expression).Compile().Invoke();
    }

}
