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

    // The configuration load path reports a rejected formula through the rule error state, which names the
    // rule and the formula, so it only logs the failure here and does not publish the generic notification.
    public static bool TryCompile<TDelegate>(
        Func<TDelegate> compile, string formula, ILogger logger)
    {
        try
        {
            _ = compile();
            return true;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to compile formula {Formula}", formula);
            return false;
        }
    }
}
