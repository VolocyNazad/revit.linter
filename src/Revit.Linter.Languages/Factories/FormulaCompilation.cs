using Microsoft.Extensions.Logging;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.Languages.Factories;

internal static class FormulaCompilation
{
    // A formula comes from a configuration file, so a faulty one must not stop the catalog from loading:
    // it is logged, reported to the user through the notifier, and replaced by the fallback.
    public static TDelegate CompileOrFallback<TDelegate>(
        Func<TDelegate> compile, TDelegate fallback, string formula,
        ILogger logger, IFormulaCompilationNotifier notifier)
    {
        try
        {
            return compile();
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to compile formula {Formula}", formula);
            notifier.Notify();
            return fallback;
        }
    }
}
