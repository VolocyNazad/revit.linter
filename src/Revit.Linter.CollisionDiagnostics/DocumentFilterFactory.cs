using Microsoft.Extensions.Logging;
using Revit.Linter.Languages.Languages;
using StringToExpression;
using System.Linq.Expressions;

namespace Revit.Linter.CollisionDiagnostics;

public class DocumentFilterFactory(
    ILogger<DocumentFilterFactory> logger,
    IFormulaCompilationNotifier notifier)
{
    private static readonly ParameterExpression _documentExpression = Expression.Parameter(typeof(Document));

    public Func<Document, bool> Create(string formula) => CreateDelegate(formula);

    private static Language Language => field ??= new(LanguageDefinitions.CreateForDocument(_documentExpression));
    private Func<Document, bool> CreateDelegate(string formula)
    {
        try
        {
            Expression body = Language.Parse(formula);
            return Expression.Lambda<Func<Document, bool>>(body, _documentExpression).Compile();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Collision diagnostic formula compilation error.");
            notifier.Notify();
            return doc => false;
        }
    }
}
