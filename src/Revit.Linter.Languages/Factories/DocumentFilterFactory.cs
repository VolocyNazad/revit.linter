using Microsoft.Extensions.Logging;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.Languages.Languages;
using StringToExpression;
using System.Linq.Expressions;

namespace Revit.Linter.Languages.Factories;

/// <summary>Compiles document formulas into predicates over a Revit document.</summary>
/// <param name="logger">Receives a warning for a formula that cannot be compiled.</param>
/// <param name="notifier">Tells the user that a formula could not be compiled.</param>
public sealed class DocumentFilterFactory(
    ILogger<DocumentFilterFactory> logger,
    IFormulaCompilationNotifier notifier)
{
    private static readonly ParameterExpression DocumentParameter = Expression.Parameter(typeof(Document));

    private static Language Language => field ??= new(LanguageDefinitions.CreateForDocument(DocumentParameter));

    /// <summary>Compiles <paramref name="formula"/> into a document predicate.</summary>
    /// <param name="formula">The formula in the document grammar.</param>
    /// <returns>The compiled predicate.</returns>
    /// <remarks>
    /// A formula that cannot be compiled is logged and reported through the notifier, and the returned
    /// predicate rejects every document.
    /// </remarks>
    public Func<Document, bool> Create(string formula) => FormulaCompilation.CompileOrFallback<Func<Document, bool>>(
        () => Expression.Lambda<Func<Document, bool>>(Language.Parse(formula), DocumentParameter).Compile(),
        _ => false, formula, logger, notifier);
}
